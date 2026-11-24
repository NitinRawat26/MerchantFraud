using System.Globalization;
using System.Text.Json;
using MerchantFraud.Core.Domain;
using MerchantFraud.Core.Scoring;
using MerchantFraud.Features;
using MerchantFraud.Ingestion;
using MerchantFraud.Integrations;
using MerchantFraud.Ml;
using MerchantFraud.Network;
using MerchantFraud.Persistence;
using MerchantFraud.Rules;
using MerchantFraud.Scoring;
using MerchantFraud.Synthetic;

namespace MerchantFraud.Presets;

public static class FraudPresetRunner
{
    public const string ChampionVersion = "logreg-2026.01";
    public const string ChallengerVersion = "logreg-2026.02";
    private const string Actor = "preset";

    public static FraudPresetRun Run(FraudPreset preset, FraudStore? store = null)
    {
        ArgumentNullException.ThrowIfNull(preset);

        var asOf = DemoMerchantScenarios.AsOf;
        var demos = DemoMerchantScenarios.All
            .Where(demo => preset.MerchantIds.Contains(demo.Merchant.Id, StringComparer.Ordinal))
            .ToArray();
        var names = demos.ToDictionary(demo => demo.Merchant.Id, demo => demo.Merchant.TradingName ?? demo.Merchant.LegalName, StringComparer.Ordinal);
        var stages = new List<PresetStage>();

        // 1. Synthetic labelled training data.
        var baselineDataset = SyntheticDataGenerator.Generate(merchantCount: 60, seed: 42, asOf: asOf);
        var refreshDataset = SyntheticDataGenerator.Generate(merchantCount: 60, seed: 7, asOf: asOf);
        stages.Add(SyntheticStage(baselineDataset, refreshDataset));

        // 2. MapWright partner-schema mapping.
        var feed = DemoMerchantScenarios.BuildFeed(preset.MerchantIds);
        var mappedRows = feed.Rows.Select(row => MapWrightTransactionMapper.Map(row, feed.Profile)).ToArray();
        stages.Add(MapWrightStage(feed, mappedRows));

        // 3. Ingestion, validation and PII/PCI masking.
        var feedJson = JsonSerializer.Serialize(new { transactions = mappedRows });
        var ingestion = TransactionFeedParser.ParseJson(feedJson);
        var transactions = ingestion.Transactions;
        stages.Add(IngestionStage(feed, mappedRows, ingestion));

        // 4. Features.
        var features = demos.ToDictionary(
            demo => demo.Merchant.Id,
            demo => MerchantFeatureExtractor.Extract(demo.Merchant.Id, transactions, feed.Disputes, currency: "USD", asOf: asOf),
            StringComparer.Ordinal);
        stages.Add(FeatureStage(demos, features));

        // 5. Rules.
        var rules = features.ToDictionary(pair => pair.Key, pair => FraudRuleEngine.Evaluate(pair.Value), StringComparer.Ordinal);
        stages.Add(RuleStage(demos, rules, names));

        // 6. Network.
        var network = MerchantNetworkAnalyzer.Analyze(demos.Select(demo => demo.Merchant), transactions);
        stages.Add(NetworkStage(network, names));

        // 7. MerchantIntelligence onboarding signals.
        var miSignals = demos.ToDictionary(demo => demo.Merchant.Id, demo => MerchantIntelligenceSignalMapper.Map(demo.Assessment), StringComparer.Ordinal);
        stages.Add(MerchantIntelligenceStage(demos, miSignals));

        // 8. ML training, champion/challenger and drift.
        var baselineExamples = FraudDatasetBuilder.Build(baselineDataset, asOf);
        var refreshExamples = FraudDatasetBuilder.Build(refreshDataset, asOf);
        var champion = FraudModelTrainer.Train(baselineExamples, ChampionVersion);
        var challenger = FraudModelTrainer.Train(refreshExamples, ChallengerVersion);
        var registry = new FraudModelRegistry();
        registry.Register(new FraudModelVersion(ChampionVersion, "Logistic regression (baseline)", FraudModelStatus.Champion, asOf.AddDays(-30), champion.Metrics));
        registry.Register(new FraudModelVersion(ChallengerVersion, "Logistic regression (refresh)", FraudModelStatus.Challenger, asOf, challenger.Metrics));
        var promoted = challenger.Metrics.F1 > champion.Metrics.F1;
        if (promoted)
        {
            registry.Promote(ChallengerVersion);
        }

        var activeModel = promoted ? challenger : champion;
        var demoExamples = demos
            .Select(demo => new FraudTrainingExample(demo.Merchant.Id, asOf, demo.ExpectedFraud, null, FraudFeatureMapper.Map(features[demo.Merchant.Id])))
            .ToArray();
        var drift = FraudDriftAnalyzer.Analyze(baselineExamples, demoExamples);
        var probabilities = demoExamples.ToDictionary(
            example => example.MerchantId,
            example => activeModel.Model.PredictProbability(example.Features),
            StringComparer.Ordinal);
        stages.Add(MlStage(champion, challenger, registry, promoted, drift, probabilities, names));

        // 9. Scoring.
        var scoring = new Dictionary<string, MerchantScoringResult>(StringComparer.Ordinal);
        foreach (var demo in demos)
        {
            var merchantId = demo.Merchant.Id;
            var additional = new List<ScoreSignal>(miSignals[merchantId]);
            var networkScore = network.Scores.FirstOrDefault(score => score.MerchantId == merchantId);
            if (networkScore is not null)
            {
                additional.Add(new ScoreSignal(
                    "NETWORK_LINKED_MERCHANTS",
                    $"Linked to {networkScore.LinkedMerchantCount} merchant(s) through {networkScore.LinkCount} shared identifier(s).",
                    Math.Clamp(networkScore.RiskScore / 5, 0, 200)));
            }

            var probability = probabilities[merchantId];
            if (probability >= 0.8)
            {
                additional.Add(new ScoreSignal("ML_FRAUD_PROBABILITY_HIGH", $"Model {activeModel.Version} fraud probability is {probability:P0}.", 100));
            }
            else if (probability >= 0.5)
            {
                additional.Add(new ScoreSignal("ML_FRAUD_PROBABILITY_ELEVATED", $"Model {activeModel.Version} fraud probability is {probability:P0}.", 50));
            }

            scoring[merchantId] = MerchantScoringEngine.Score(features[merchantId], rules[merchantId], activeModel.Version, asOf, additional);
        }

        stages.Add(ScoringStage(demos, scoring, names));

        // 10. Persistence, alerts and cases.
        var cases = scoring.Values
            .Where(result => result.ScoreResult.Score.Value >= FraudRiskBands.HighThreshold)
            .Select(result => new FraudCase(
                $"CASE-PRESET-{result.ScoreResult.SubjectId}",
                result.ScoreResult.SubjectId,
                $"{names[result.ScoreResult.SubjectId]}: {result.ScoreResult.Score.Band} fraud risk ({result.ScoreResult.Score.Value})",
                result.ScoreResult.Score.Band == FraudRiskBand.Critical ? CasePriority.Critical : CasePriority.High,
                CaseStatus.Open,
                "fraud-analyst",
                asOf,
                externalRef: $"preset:{preset.Id}"))
            .OrderBy(fraudCase => fraudCase.MerchantId, StringComparer.Ordinal)
            .ToArray();
        stages.Add(PersistenceStage(preset, store, demos, transactions, feed.Disputes, scoring, cases, stages));

        var outcomes = demos
            .Select(demo =>
            {
                var merchantId = demo.Merchant.Id;
                var result = scoring[merchantId];
                return new PresetMerchantOutcome(
                    merchantId,
                    names[merchantId],
                    demo.Scenario,
                    result.ScoreResult.Score.Value,
                    result.ScoreResult.Score.Band.ToString(),
                    result.RuleEvaluation.RecommendedAction.ToString(),
                    result.RuleEvaluation.Hits.Count,
                    network.Scores.FirstOrDefault(score => score.MerchantId == merchantId)?.LinkedMerchantCount ?? 0,
                    Math.Round(probabilities[merchantId], 4),
                    demo.Assessment.Decision.ToString(),
                    result.ScoreResult.Score.ReasonCodes,
                    cases.FirstOrDefault(fraudCase => fraudCase.MerchantId == merchantId)?.Id);
            })
            .OrderByDescending(outcome => outcome.Score)
            .ThenBy(outcome => outcome.MerchantId, StringComparer.Ordinal)
            .ToArray();

        return new FraudPresetRun(preset.Id, preset.Name, preset.Description, preset.FocusMerchantId, asOf, store is not null, outcomes, stages);
    }

    private static PresetStage SyntheticStage(SyntheticDataset baseline, SyntheticDataset refresh)
    {
        var rows = baseline.Merchants
            .GroupBy(scenario => scenario.Label.FraudType)
            .OrderBy(group => group.Key)
            .Select(group => Row(
                group.Key.ToString(),
                group.Count(),
                group.Sum(scenario => scenario.Transactions.Count),
                group.Sum(scenario => scenario.Disputes.Count),
                group.First().Label.Rationale))
            .ToArray();

        return new PresetStage(
            1,
            "synthetic",
            "Synthetic training data",
            "MerchantFraud.Synthetic",
            "ok",
            $"Generated {baseline.MerchantCount} labelled baseline merchants (seed 42) and {refresh.MerchantCount} refresh merchants (seed 7) to train and challenge the ML model.",
            new[]
            {
                Metric("Merchants", baseline.MerchantCount),
                Metric("Transactions", baseline.TransactionCount),
                Metric("Disputes", baseline.DisputeCount),
                Metric("Fraud-labelled", baseline.FraudulentMerchantCount)
            },
            new[] { Table("Baseline labels", new[] { "Label", "Merchants", "Transactions", "Disputes", "Rationale" }, rows) });
    }

    private static PresetStage MapWrightStage(DemoPartnerFeed feed, IReadOnlyList<IReadOnlyDictionary<string, string?>> mapped)
    {
        var mappingRows = feed.Profile.FieldMappings
            .Select(mapping => Row(mapping.Value, mapping.Key))
            .ToArray();
        var sampleRows = feed.Rows
            .Zip(mapped)
            .GroupBy(pair => pair.First["mid"])
            .Select(group => group.First())
            .Select(pair => Row(
                pair.First["txn_ref"],
                pair.First["mid"],
                $"amt={pair.First["amt"]}, entry_mode={pair.First["entry_mode"]}, result={pair.First["result"]}",
                $"amount={Get(pair.Second, "amount")}, channel={Get(pair.Second, "channel")}, status={Get(pair.Second, "status")}"))
            .ToArray();

        return new PresetStage(
            2,
            "mapwright",
            "Partner schema mapping",
            "MerchantFraud.Integrations · MapWright",
            "ok",
            $"Mapped {mapped.Count} partner rows from the '{feed.Profile.Name}' gateway schema into canonical MerchantFraud transaction fields.",
            new[]
            {
                Metric("Partner rows", feed.Rows.Count),
                Metric("Field mappings", feed.Profile.FieldMappings.Count),
                Metric("Profile", feed.Profile.Name)
            },
            new[]
            {
                Table("Field mappings", new[] { "Partner field", "Canonical field" }, mappingRows),
                Table("Sample rows", new[] { "Transaction", "Merchant", "Partner values", "Canonical values" }, sampleRows)
            });
    }

    private static PresetStage IngestionStage(
        DemoPartnerFeed feed,
        IReadOnlyList<IReadOnlyDictionary<string, string?>> mapped,
        TransactionFeedResult ingestion)
    {
        var byId = ingestion.Transactions.ToDictionary(transaction => transaction.Id, StringComparer.Ordinal);
        var maskingRows = mapped
            .Where(row => byId.ContainsKey(Get(row, "id")))
            .GroupBy(row => Get(row, "merchantId"))
            .Select(group => group.First())
            .Select(row =>
            {
                var stored = byId[Get(row, "id")];
                var pan = Get(row, "cardNumber");
                return Row(
                    stored.MerchantId,
                    Get(row, "ipAddress"),
                    stored.IpAddress,
                    pan.Length == 0 ? "—" : $"{pan[..6]}••••••{pan[^4..]} (full PAN in feed)",
                    stored.CardLast4);
            })
            .ToArray();
        var perMerchant = ingestion.Transactions
            .GroupBy(transaction => transaction.MerchantId)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => Row(
                group.Key,
                group.Count(),
                group.Count(transaction => transaction.Status == TransactionStatus.Declined),
                feed.Disputes.Count(dispute => dispute.MerchantId == group.Key)))
            .ToArray();

        return new PresetStage(
            3,
            "ingestion",
            "Ingestion & PII/PCI masking",
            "MerchantFraud.Ingestion",
            ingestion.Errors.Count == 0 ? "ok" : "warning",
            $"Parsed {ingestion.Transactions.Count} of {mapped.Count} rows; {ingestion.Errors.Count} row(s) rejected with row-level errors. IPs are truncated to /16 and full PANs are reduced to last 4.",
            new[]
            {
                Metric("Rows received", mapped.Count),
                Metric("Stored", ingestion.Transactions.Count),
                Metric("Rejected", ingestion.Errors.Count),
                Metric("Disputes loaded", feed.Disputes.Count)
            },
            new[]
            {
                Table("Masking examples", new[] { "Merchant", "Raw IP", "Stored IP", "Card in feed", "Stored last 4" }, maskingRows),
                Table("Accepted by merchant", new[] { "Merchant", "Transactions", "Declined", "Disputes" }, perMerchant),
                Table("Row-level errors", new[] { "Error" }, ingestion.Errors.Select(error => Row(error)).ToArray())
            });
    }

    private static PresetStage FeatureStage(IReadOnlyList<DemoMerchant> demos, IReadOnlyDictionary<string, MerchantFeatureSet> features)
    {
        var rows = demos
            .Select(demo => features[demo.Merchant.Id])
            .Select(set => Row(
                set.MerchantId,
                set.TransactionCount,
                Percent(set.DeclineRate),
                Percent(set.RefundRate),
                Percent(set.DisputeRate),
                Percent(set.CardNotPresentShare),
                Percent(set.RoundAmountShare),
                set.TransactionsLast24Hours,
                Money(set.AverageTicket),
                Ratio(set.MaxTicketToAverageRatio)))
            .ToArray();

        return new PresetStage(
            4,
            "features",
            "Merchant features",
            "MerchantFraud.Features",
            "ok",
            $"Extracted point-in-time behaviour features for {demos.Count} merchant(s) as of {DemoMerchantScenarios.AsOf:yyyy-MM-dd HH:mm} UTC.",
            new[]
            {
                Metric("Merchants", demos.Count),
                Metric("Transactions", features.Values.Sum(set => set.TransactionCount)),
                Metric("Volume", Money(features.Values.Sum(set => set.TotalVolume)))
            },
            new[]
            {
                Table(
                    "Feature sets",
                    new[] { "Merchant", "Transactions", "Decline rate", "Refund rate", "Dispute rate", "CNP share", "Round amounts", "Last 24h", "Avg ticket", "Max/avg" },
                    rows)
            });
    }

    private static PresetStage RuleStage(
        IReadOnlyList<DemoMerchant> demos,
        IReadOnlyDictionary<string, FraudRuleEvaluation> rules,
        IReadOnlyDictionary<string, string> names)
    {
        var hitRows = demos
            .SelectMany(demo => rules[demo.Merchant.Id].Hits.Select(hit => Row(demo.Merchant.Id, hit.RuleName, hit.Severity, hit.Action, hit.Message)))
            .ToArray();
        var summaryRows = demos
            .Select(demo => rules[demo.Merchant.Id])
            .Select(evaluation => Row(evaluation.MerchantId, names[evaluation.MerchantId], evaluation.Hits.Count, evaluation.HasHits ? evaluation.HighestSeverity.ToString() : "—", evaluation.RecommendedAction))
            .ToArray();

        return new PresetStage(
            5,
            "rules",
            "Deterministic rules",
            "MerchantFraud.Rules",
            hitRows.Length == 0 ? "ok" : "warning",
            $"Evaluated {FraudRuleCatalog.Default.Count} catalog rules per merchant; {hitRows.Length} rule hit(s) across {summaryRows.Count(row => row[2] != "0")} merchant(s).",
            new[]
            {
                Metric("Rules in catalog", FraudRuleCatalog.Default.Count),
                Metric("Rule hits", hitRows.Length),
                Metric("Decline actions", rules.Values.Count(evaluation => evaluation.RecommendedAction == FraudRuleAction.Decline))
            },
            new[]
            {
                Table("Recommended action", new[] { "Merchant", "Name", "Hits", "Highest severity", "Action" }, summaryRows),
                Table("Rule hits", new[] { "Merchant", "Rule", "Severity", "Action", "Message" }, hitRows)
            });
    }

    private static PresetStage NetworkStage(MerchantNetworkAnalysis network, IReadOnlyDictionary<string, string> names)
    {
        var scoreRows = network.Scores
            .Select(score => Row(score.MerchantId, names.GetValueOrDefault(score.MerchantId, score.MerchantId), score.LinkedMerchantCount, score.LinkCount, score.RiskScore, string.Join(", ", score.ReasonCodes)))
            .ToArray();
        var linkRows = network.Links
            .Select(link => Row(link.MerchantAId, link.MerchantBId, link.LinkType, link.SharedValue, link.SharedTransactionCount))
            .ToArray();

        return new PresetStage(
            6,
            "network",
            "Merchant network analysis",
            "MerchantFraud.Network",
            network.Links.Count == 0 ? "ok" : "warning",
            network.Links.Count == 0
                ? "No merchants share devices, IP blocks, shoppers or card fingerprints."
                : $"Found {network.Links.Count} shared-identifier link(s) connecting {network.Scores.Count} merchant(s).",
            new[]
            {
                Metric("Links", network.Links.Count),
                Metric("Linked merchants", network.Scores.Count),
                Metric("Max network score", network.Scores.Count == 0 ? 0 : network.Scores.Max(score => score.RiskScore))
            },
            new[]
            {
                Table("Network scores", new[] { "Merchant", "Name", "Linked merchants", "Links", "Network score", "Reason codes" }, scoreRows),
                Table("Shared identifiers", new[] { "Merchant A", "Merchant B", "Link type", "Shared value", "Shared transactions" }, linkRows)
            });
    }

    private static PresetStage MerchantIntelligenceStage(
        IReadOnlyList<DemoMerchant> demos,
        IReadOnlyDictionary<string, IReadOnlyList<ScoreSignal>> signals)
    {
        var assessmentRows = demos
            .Select(demo => Row(
                demo.Merchant.Id,
                demo.Assessment.AssessmentId,
                demo.Assessment.Decision,
                demo.Assessment.RiskScore,
                demo.Assessment.ReasonCodes.Count == 0 ? "—" : string.Join(", ", demo.Assessment.ReasonCodes)))
            .ToArray();
        var signalRows = demos
            .SelectMany(demo => signals[demo.Merchant.Id].Select(signal => Row(demo.Merchant.Id, signal.Code, signal.Description, signal.Points)))
            .ToArray();

        return new PresetStage(
            7,
            "merchant-intelligence",
            "MerchantIntelligence onboarding signals",
            "MerchantFraud.Integrations · MerchantIntelligence",
            "ok",
            $"Mapped {demos.Count} pre-boarding KYB/credit assessment(s) into {signalRows.Length} fraud score signal(s).",
            new[]
            {
                Metric("Assessments", demos.Count),
                Metric("Declined at onboarding", demos.Count(demo => demo.Assessment.Decision == MerchantIntelligenceDecision.Decline)),
                Metric("Referred", demos.Count(demo => demo.Assessment.Decision == MerchantIntelligenceDecision.Refer)),
                Metric("Signals", signalRows.Length)
            },
            new[]
            {
                Table("Assessments", new[] { "Merchant", "Assessment", "Decision", "Risk score", "Reason codes" }, assessmentRows),
                Table("Mapped signals", new[] { "Merchant", "Signal", "Description", "Points" }, signalRows)
            });
    }

    private static PresetStage MlStage(
        TrainedFraudModel champion,
        TrainedFraudModel challenger,
        FraudModelRegistry registry,
        bool promoted,
        FraudDriftReport drift,
        IReadOnlyDictionary<string, double> probabilities,
        IReadOnlyDictionary<string, string> names)
    {
        var active = registry.Champion()!;
        var metricRows = new[] { champion, challenger }
            .Select(model => Row(model.Version, Percent(model.Metrics.Precision), Percent(model.Metrics.Recall), Percent(model.Metrics.F1), Percent(model.Metrics.Accuracy), model.Metrics.Total))
            .ToArray();
        var registryRows = registry.Versions
            .Select(version => Row(version.Version, version.Name, version.Status, version.TrainedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)))
            .ToArray();
        var predictionRows = probabilities
            .OrderByDescending(pair => pair.Value)
            .Select(pair => Row(pair.Key, names[pair.Key], Percent(pair.Value), pair.Value >= 0.8 ? "High" : pair.Value >= 0.5 ? "Elevated" : "Low"))
            .ToArray();
        var driftRows = drift.Features
            .Select(feature => Row(feature.FeatureName, Number(feature.BaselineMean), Number(feature.CurrentMean), Number(feature.MeanShift), feature.IsDrifted ? "Drifted" : "Stable"))
            .ToArray();

        return new PresetStage(
            8,
            "ml",
            "ML scoring & model ops",
            "MerchantFraud.Ml",
            drift.HasSignificantDrift ? "warning" : "ok",
            $"Trained {champion.Version} and {challenger.Version} on synthetic data; {(promoted ? $"{challenger.Version} was promoted to champion" : $"{champion.Version} stays champion")}. {drift.Features.Count(feature => feature.IsDrifted)} feature(s) drifted between training data and live preset traffic.",
            new[]
            {
                Metric("Champion", active.Version),
                Metric("Champion F1", Percent(active.Metrics?.F1 ?? 0)),
                Metric("Drifted features", drift.Features.Count(feature => feature.IsDrifted)),
                Metric("Merchants scored", probabilities.Count)
            },
            new[]
            {
                Table("Holdout metrics", new[] { "Model", "Precision", "Recall", "F1", "Accuracy", "Test merchants" }, metricRows),
                Table("Model registry", new[] { "Version", "Name", "Status", "Trained" }, registryRows),
                Table("Fraud probability", new[] { "Merchant", "Name", "Probability", "Level" }, predictionRows),
                Table("Feature drift", new[] { "Feature", "Training mean", "Preset mean", "Shift", "Status" }, driftRows)
            });
    }

    private static PresetStage ScoringStage(
        IReadOnlyList<DemoMerchant> demos,
        IReadOnlyDictionary<string, MerchantScoringResult> scoring,
        IReadOnlyDictionary<string, string> names)
    {
        var scoreRows = scoring.Values
            .OrderByDescending(result => result.ScoreResult.Score.Value)
            .Select(result => Row(result.ScoreResult.SubjectId, names[result.ScoreResult.SubjectId], result.ScoreResult.Score.Value, result.ScoreResult.Score.Band, result.RawScore, result.RuleEvaluation.RecommendedAction))
            .ToArray();
        var signalRows = demos
            .SelectMany(demo => scoring[demo.Merchant.Id].Signals.Select(signal => Row(demo.Merchant.Id, signal.Code, signal.Description, signal.Points)))
            .ToArray();

        return new PresetStage(
            9,
            "scoring",
            "Explainable 0–1000 scoring",
            "MerchantFraud.Scoring",
            scoring.Values.Any(result => result.ScoreResult.Score.Value >= FraudRiskBands.HighThreshold) ? "warning" : "ok",
            $"Combined rule hits, feature signals, MerchantIntelligence, network and ML signals into capped scores; {scoring.Values.Count(result => result.ScoreResult.Score.Value >= FraudRiskBands.HighThreshold)} merchant(s) scored High or Critical.",
            new[]
            {
                Metric("Merchants scored", scoring.Count),
                Metric("Critical", scoring.Values.Count(result => result.ScoreResult.Score.Band == FraudRiskBand.Critical)),
                Metric("High", scoring.Values.Count(result => result.ScoreResult.Score.Band == FraudRiskBand.High)),
                Metric("Signals", signalRows.Length)
            },
            new[]
            {
                Table("Scores", new[] { "Merchant", "Name", "Score", "Band", "Raw points", "Rule action" }, scoreRows),
                Table("Score signals", new[] { "Merchant", "Signal", "Description", "Points" }, signalRows)
            });
    }

    private static PresetStage PersistenceStage(
        FraudPreset preset,
        FraudStore? store,
        IReadOnlyList<DemoMerchant> demos,
        IReadOnlyList<FraudTransaction> transactions,
        IReadOnlyList<FraudDispute> disputes,
        IReadOnlyDictionary<string, MerchantScoringResult> scoring,
        IReadOnlyList<FraudCase> cases,
        IReadOnlyList<PresetStage> completedStages)
    {
        var alertRows = scoring.Values
            .Where(result => result.ScoreResult.Score.Value >= FraudRiskBands.HighThreshold)
            .OrderByDescending(result => result.ScoreResult.Score.Value)
            .Select(result => Row(result.ScoreResult.SubjectId, result.ScoreResult.Score.Value, result.ScoreResult.Score.Band, string.Join(", ", result.ScoreResult.Score.ReasonCodes.Take(6))))
            .ToArray();
        var caseRows = cases.Select(fraudCase => Row(fraudCase.Id, fraudCase.MerchantId, fraudCase.Title, fraudCase.Priority, fraudCase.Status, fraudCase.AssignedTo)).ToArray();
        var auditEvents = 0;
        var newCaseCount = 0;

        if (store is not null)
        {
            var existingCaseIds = store.GetCases().Select(fraudCase => fraudCase.Id).ToHashSet(StringComparer.Ordinal);

            foreach (var demo in demos)
            {
                store.UpsertMerchant(demo.Merchant);
            }

            foreach (var transaction in transactions)
            {
                store.UpsertTransaction(transaction);
            }

            foreach (var dispute in disputes)
            {
                store.UpsertDispute(dispute);
            }

            foreach (var result in scoring.Values)
            {
                store.SaveScoreResult(result.ScoreResult);
            }

            foreach (var fraudCase in cases)
            {
                if (existingCaseIds.Contains(fraudCase.Id))
                {
                    continue;
                }

                store.UpsertCase(fraudCase);
                store.AddAuditEvent(Actor, "open_case", "case", fraudCase.Id, $"preset={preset.Id};merchant={fraudCase.MerchantId}");
                newCaseCount++;
                auditEvents++;
            }

            foreach (var stage in completedStages)
            {
                store.AddAuditEvent(Actor, $"stage_{stage.Key}", "preset", preset.Id, stage.Summary);
                auditEvents++;
            }
        }

        return new PresetStage(
            10,
            "persistence",
            "Alerts, cases & audit trail",
            "MerchantFraud.Persistence · Api",
            store is null ? "skipped" : "ok",
            store is null
                ? $"Dry run: {alertRows.Length} of this preset's merchant(s) would raise an alert and {cases.Count} case(s) would be opened; nothing was written."
                : $"Saved this preset's merchants, transactions, disputes and scores to SQLite. {alertRows.Length} of its merchant(s) scored High or Critical and are in the Alerts queue; {newCaseCount} new case(s) opened, {cases.Count - newCaseCount} already open.",
            new[]
            {
                Metric("Merchants saved", store is null ? 0 : demos.Count),
                Metric("Transactions saved", store is null ? 0 : transactions.Count),
                Metric("Alerts", alertRows.Length),
                Metric("Cases opened", newCaseCount),
                Metric("Cases already open", store is null ? 0 : cases.Count - newCaseCount),
                Metric("Audit events", auditEvents)
            },
            new[]
            {
                Table("Alerts (score ≥ 500)", new[] { "Merchant", "Score", "Band", "Top reason codes" }, alertRows),
                Table("Cases", new[] { "Case", "Merchant", "Title", "Priority", "Status", "Assignee" }, caseRows)
            });
    }

    private static PresetMetric Metric(string label, object value) => new(label, Format(value));

    private static PresetTable Table(string title, IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<string>> rows) => new(title, columns, rows);

    private static IReadOnlyList<string> Row(params object?[] values) => values.Select(Format).ToArray();

    private static string Format(object? value) => value switch
    {
        null => "—",
        string text => text,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    private static string Get(IReadOnlyDictionary<string, string?> row, string key) =>
        row.TryGetValue(key, out var value) && value is not null ? value : string.Empty;

    private static string Percent(decimal value) => Percent((double)value);

    private static string Percent(double value) => value.ToString("0.0%", CultureInfo.InvariantCulture);

    private static string Money(decimal value) => value.ToString("$#,##0.00", CultureInfo.InvariantCulture);

    private static string Ratio(decimal value) => value.ToString("0.0x", CultureInfo.InvariantCulture);

    private static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
