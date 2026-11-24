using MerchantFraud.Ml;
using MerchantFraud.Synthetic;

namespace MerchantFraud.Tests.Ml;

public class MerchantFraudMlTests
{
    [Fact]
    public void DatasetBuilder_BuildsFeatureExamples()
    {
        var dataset = SyntheticDataGenerator.Generate(merchantCount: 12, seed: 42);

        var examples = FraudDatasetBuilder.Build(dataset);

        Assert.Equal(12, examples.Count);
        Assert.Equal(FraudFeatureMapper.FeatureCount, examples[0].Features.Count);
        Assert.Contains(examples, example => example.IsFraudulent);
        Assert.Contains(examples, example => !example.IsFraudulent);
        Assert.All(examples, example => Assert.All(example.Features, feature => Assert.True(double.IsFinite(feature))));
    }

    [Fact]
    public void TrainTestSplit_UsesTimeOrder()
    {
        var examples = FraudDatasetBuilder.Build(SyntheticDataGenerator.Generate(merchantCount: 20, seed: 3));

        var split = FraudDatasetSplitter.Split(examples, trainFraction: 0.7);

        Assert.Equal(20, split.Train.Count + split.Test.Count);
        Assert.Equal(14, split.Train.Count);
        Assert.Equal(6, split.Test.Count);
        Assert.True(split.Train.Max(example => example.AsOf) <= split.Test.Min(example => example.AsOf));
    }

    [Fact]
    public void LogisticRegression_LearnsSyntheticFraudSignal()
    {
        var examples = FraudDatasetBuilder.Build(SyntheticDataGenerator.Generate(merchantCount: 60, seed: 42));
        var split = FraudDatasetSplitter.Split(examples, trainFraction: 0.7);
        var model = LogisticRegressionFraudModel.Fit(split.Train, learningRate: 0.5, epochs: 400);

        var probabilities = split.Test.Select(example => model.PredictProbability(example.Features)).ToArray();
        var metrics = FraudModelMetrics.Evaluate(split.Test, probabilities, threshold: 0.5);
        var fraudAverage = split.Test
            .Select((example, index) => (example.IsFraudulent, probabilities[index]))
            .Where(item => item.IsFraudulent)
            .Select(item => item.Item2)
            .Average();
        var legitimateAverage = split.Test
            .Select((example, index) => (example.IsFraudulent, probabilities[index]))
            .Where(item => !item.IsFraudulent)
            .Select(item => item.Item2)
            .Average();

        Assert.True(metrics.Recall > 0.4, $"Recall was {metrics.Recall}.");
        Assert.True(metrics.Precision > 0.2, $"Precision was {metrics.Precision}.");
        Assert.True(fraudAverage > legitimateAverage, $"Fraud average {fraudAverage} did not exceed legitimate average {legitimateAverage}.");
        Assert.Equal("logreg-v1", model.ModelVersion);
    }
}
