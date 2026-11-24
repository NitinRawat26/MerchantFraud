using MerchantFraud.Ml;
using MerchantFraud.Synthetic;

namespace MerchantFraud.Tests.Ml;

public class ModelOpsTests
{
    [Fact]
    public void Trainer_TrainsModelAndComputesTestMetrics()
    {
        var examples = Enumerable.Range(0, 8)
            .Select(index => Example($"MER-{index}", index % 2 == 0, index * 0.1, index))
            .ToArray();

        var trained = FraudModelTrainer.Train(examples, "logreg-v2");

        Assert.Equal("logreg-v2", trained.Version);
        Assert.Equal("logreg-v1", trained.Model.ModelVersion);
        Assert.True(trained.Metrics.Total > 0);
        Assert.True(trained.Metrics.Precision >= 0);
    }

    [Fact]
    public void Registry_PromotesChallengerAndRetiresPreviousChampion()
    {
        var registry = new FraudModelRegistry();
        registry.Register(new FraudModelVersion("logreg-v1", "baseline", FraudModelStatus.Champion, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), null));
        registry.Register(new FraudModelVersion("logreg-v2", "challenger", FraudModelStatus.Challenger, new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero), null));

        var promoted = registry.Promote("logreg-v2");

        Assert.Equal(FraudModelStatus.Champion, promoted.Status);
        Assert.Equal("logreg-v2", registry.Champion()?.Version);
        Assert.Equal(FraudModelStatus.Retired, registry.Versions.Single(version => version.Version == "logreg-v1").Status);
    }

    [Fact]
    public void DriftAnalyzer_DetectsFeatureShift()
    {
        var baseline = new[] { Example("A", false, 1.0, 0), Example("B", false, 1.0, 1) };
        var current = new[] { Example("C", false, 3.0, 2), Example("D", false, 3.0, 3) };

        var report = FraudDriftAnalyzer.Analyze(baseline, current);

        Assert.True(report.HasSignificantDrift);
        var drift = report.Features[0];
        Assert.Equal("transaction_count_log", drift.FeatureName);
        Assert.True(drift.IsDrifted);
        Assert.Equal(2.0, drift.MeanShift, 3);
    }

    [Fact]
    public void DriftAnalyzer_NoDrift_WhenFeaturesStable()
    {
        var baseline = new[] { Example("A", false, 1.0, 0), Example("B", false, 1.0, 1) };
        var current = new[] { Example("C", false, 1.0, 2), Example("D", false, 1.0, 3) };

        var report = FraudDriftAnalyzer.Analyze(baseline, current);

        Assert.False(report.HasSignificantDrift);
    }

    private static FraudTrainingExample Example(string merchantId, bool fraudulent, double featureValue, int day)
    {
        var features = new double[FraudFeatureMapper.FeatureCount];
        features[0] = featureValue;
        return new FraudTrainingExample(
            merchantId,
            new DateTimeOffset(2026, 1, day + 1, 0, 0, 0, TimeSpan.Zero),
            fraudulent,
            fraudulent ? SyntheticFraudType.TransactionFraud : SyntheticFraudType.None,
            features);
    }
}
