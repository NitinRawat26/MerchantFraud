namespace MerchantFraud.Ml;

public static class FraudDriftAnalyzer
{
    public static FraudDriftReport Analyze(
        IReadOnlyList<FraudTrainingExample> baseline,
        IReadOnlyList<FraudTrainingExample> current,
        double threshold = 0.25)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(current);
        if (baseline.Count == 0 || current.Count == 0)
        {
            throw new ArgumentException("Baseline and current datasets must not be empty.");
        }

        if (threshold <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold), threshold, "Drift threshold must be greater than zero.");
        }

        var featureCount = Math.Max(baseline[0].Features.Count, current[0].Features.Count);
        var drifts = new List<FeatureDrift>(featureCount);
        for (var index = 0; index < featureCount; index++)
        {
            var baselineMean = Average(baseline, index);
            var currentMean = Average(current, index);
            var meanShift = currentMean - baselineMean;
            var scale = Math.Max(Math.Abs(baselineMean), 1.0);
            var isDrifted = Math.Abs(meanShift) / scale > threshold && Math.Abs(meanShift) > 1e-9;
            var featureName = index < FraudFeatureMapper.FeatureNames.Count
                ? FraudFeatureMapper.FeatureNames[index]
                : $"feature_{index}";
            drifts.Add(new FeatureDrift(featureName, baselineMean, currentMean, meanShift, isDrifted));
        }

        return new FraudDriftReport(drifts, drifts.Any(drift => drift.IsDrifted));
    }

    private static double Average(IReadOnlyList<FraudTrainingExample> examples, int featureIndex)
    {
        var sum = 0.0;
        foreach (var example in examples)
        {
            if (featureIndex < example.Features.Count)
            {
                sum += example.Features[featureIndex];
            }
        }

        return sum / examples.Count;
    }
}
