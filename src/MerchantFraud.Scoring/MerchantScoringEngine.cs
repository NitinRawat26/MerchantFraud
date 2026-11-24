using MerchantFraud.Core.Domain;
using MerchantFraud.Core.Scoring;
using MerchantFraud.Features;
using MerchantFraud.Rules;

namespace MerchantFraud.Scoring;

public static class MerchantScoringEngine
{
    public static MerchantScoringResult Score(
        MerchantFeatureSet features,
        FraudRuleEvaluation? ruleEvaluation = null,
        string? modelVersion = null,
        DateTimeOffset? scoredAt = null,
        IEnumerable<ScoreSignal>? additionalSignals = null)
    {
        ArgumentNullException.ThrowIfNull(features);

        var rules = ruleEvaluation ?? FraudRuleEngine.Evaluate(features);
        if (!string.Equals(rules.MerchantId, features.MerchantId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Rule evaluation merchant does not match feature merchant.", nameof(ruleEvaluation));
        }

        var signals = new List<ScoreSignal>();
        foreach (var hit in rules.Hits)
        {
            signals.Add(new ScoreSignal(hit.ReasonCode, hit.Message, RulePoints(hit.Severity)));
        }

        AddFeatureSignals(features, signals);

        if (additionalSignals is not null)
        {
            foreach (var signal in additionalSignals)
            {
                if (signal is not null && signal.Points > 0)
                {
                    signals.Add(signal);
                }
            }
        }

        var rawScore = signals.Sum(signal => signal.Points);
        var cappedScore = Math.Min(1000, rawScore);
        var effectiveScoredAt = scoredAt ?? features.AsOf;
        var riskScore = new FraudRiskScore(cappedScore, signals.Select(signal => signal.Code));
        var scoreResult = new FraudScoreResult(
            $"score-{features.MerchantId}-{effectiveScoredAt:yyyyMMddHHmmss}",
            FraudScoreSubjectType.Merchant,
            features.MerchantId,
            riskScore,
            effectiveScoredAt,
            modelVersion ?? "rules-v1");

        return new MerchantScoringResult(scoreResult, rules, features, signals.ToArray(), rawScore);
    }

    private static void AddFeatureSignals(MerchantFeatureSet features, List<ScoreSignal> signals)
    {
        AddFeatureSignal(signals, features.DisputeRate >= 0.01m, "FEATURE_DISPUTE_RATE", "Dispute rate is above 1%.", ScaledPoints(features.DisputeRate, 500m, 60));
        AddFeatureSignal(signals, features.RefundRate >= 0.05m, "FEATURE_REFUND_RATE", "Refund rate is above 5%.", ScaledPoints(features.RefundRate, 400m, 50));
        AddFeatureSignal(signals, features.DeclineRate >= 0.30m, "FEATURE_DECLINE_RATE", "Decline rate is above 30%.", ScaledPoints(features.DeclineRate, 100m, 40));
        AddFeatureSignal(signals, features.DisputeToVolumeRatio >= 0.02m, "FEATURE_DISPUTE_VOLUME", "Dispute volume is above 2% of processed volume.", ScaledPoints(features.DisputeToVolumeRatio, 500m, 40));
        AddFeatureSignal(signals, features.CardNotPresentShare >= 0.80m, "FEATURE_CNP_DOMINANT", "Card-not-present share is above 80%.", 20);
        AddFeatureSignal(signals, features.RoundAmountShare >= 0.70m, "FEATURE_ROUND_AMOUNTS", "Round-amount share is above 70%.", 20);
        AddFeatureSignal(signals, features.MaxTicketToAverageRatio >= 5m, "FEATURE_TICKET_OUTLIER", "Maximum ticket is at least 5x average ticket.", ScaledPoints(features.MaxTicketToAverageRatio, 3m, 30));
        AddFeatureSignal(signals, features.TransactionsLast24Hours >= 50, "FEATURE_VELOCITY_24H", "24-hour transaction velocity is elevated.", VelocityPoints(features.TransactionsLast24Hours));
    }

    private static void AddFeatureSignal(List<ScoreSignal> signals, bool condition, string code, string description, int points)
    {
        if (condition && points > 0)
        {
            signals.Add(new ScoreSignal(code, description, points));
        }
    }

    private static int RulePoints(FraudRuleSeverity severity) => severity switch
    {
        FraudRuleSeverity.Critical => 250,
        FraudRuleSeverity.High => 120,
        FraudRuleSeverity.Medium => 60,
        FraudRuleSeverity.Low => 25,
        _ => 0
    };

    private static int ScaledPoints(decimal value, decimal scale, int max)
    {
        if (value <= 0m)
        {
            return 0;
        }

        var points = Math.Round(value * scale, 0, MidpointRounding.AwayFromZero);
        return points >= max ? max : (int)points;
    }

    private static int VelocityPoints(int transactionsLast24Hours) => transactionsLast24Hours switch
    {
        >= 200 => 40,
        >= 100 => 30,
        >= 50 => 20,
        _ => 0
    };
}
