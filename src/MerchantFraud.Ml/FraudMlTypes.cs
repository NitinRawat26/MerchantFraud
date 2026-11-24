using MerchantFraud.Synthetic;

namespace MerchantFraud.Ml;

public sealed record FraudTrainingExample(
    string MerchantId,
    DateTimeOffset AsOf,
    bool IsFraudulent,
    SyntheticFraudType? FraudType,
    IReadOnlyList<double> Features);

public sealed record FraudTrainTestSplit(IReadOnlyList<FraudTrainingExample> Train, IReadOnlyList<FraudTrainingExample> Test);

public static class FraudDatasetSplitter
{
    public static FraudTrainTestSplit Split(IReadOnlyList<FraudTrainingExample> examples, double trainFraction = 0.7)
    {
        ArgumentNullException.ThrowIfNull(examples);
        if (examples.Count < 2)
        {
            throw new ArgumentException("At least two examples are required for a train/test split.", nameof(examples));
        }

        if (trainFraction is <= 0 or >= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(trainFraction), trainFraction, "Train fraction must be between 0 and 1.");
        }

        var ordered = examples
            .OrderBy(example => example.AsOf)
            .ThenBy(example => example.MerchantId, StringComparer.Ordinal)
            .ToArray();
        var trainCount = (int)Math.Round(ordered.Length * trainFraction, MidpointRounding.AwayFromZero);
        trainCount = Math.Clamp(trainCount, 1, ordered.Length - 1);

        return new FraudTrainTestSplit(
            ordered.Take(trainCount).ToArray(),
            ordered.Skip(trainCount).ToArray());
    }
}

public sealed record FraudModelMetrics(
    int Total,
    int Positives,
    int TruePositives,
    int FalsePositives,
    int FalseNegatives,
    int TrueNegatives,
    double Precision,
    double Recall,
    double F1,
    double Accuracy)
{
    public static FraudModelMetrics Evaluate(IReadOnlyList<FraudTrainingExample> examples, IReadOnlyList<double> probabilities, double threshold = 0.5)
    {
        ArgumentNullException.ThrowIfNull(examples);
        ArgumentNullException.ThrowIfNull(probabilities);
        if (examples.Count != probabilities.Count)
        {
            throw new ArgumentException("Examples and probabilities must have the same length.", nameof(probabilities));
        }

        var truePositives = 0;
        var falsePositives = 0;
        var falseNegatives = 0;
        var trueNegatives = 0;

        for (var index = 0; index < examples.Count; index++)
        {
            var predicted = probabilities[index] >= threshold;
            var actual = examples[index].IsFraudulent;
            if (predicted && actual) truePositives++;
            else if (predicted && !actual) falsePositives++;
            else if (!predicted && actual) falseNegatives++;
            else trueNegatives++;
        }

        var precision = truePositives + falsePositives == 0 ? 0 : (double)truePositives / (truePositives + falsePositives);
        var recall = truePositives + falseNegatives == 0 ? 0 : (double)truePositives / (truePositives + falseNegatives);
        var f1 = precision + recall == 0 ? 0 : 2 * precision * recall / (precision + recall);
        var accuracy = examples.Count == 0 ? 0 : (double)(truePositives + trueNegatives) / examples.Count;
        var positives = examples.Count(example => example.IsFraudulent);

        return new FraudModelMetrics(
            examples.Count,
            positives,
            truePositives,
            falsePositives,
            falseNegatives,
            trueNegatives,
            precision,
            recall,
            f1,
            accuracy);
    }
}
