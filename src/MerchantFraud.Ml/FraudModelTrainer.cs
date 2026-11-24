namespace MerchantFraud.Ml;

public static class FraudModelTrainer
{
    public static TrainedFraudModel Train(
        IReadOnlyList<FraudTrainingExample> examples,
        string version,
        double trainFraction = 0.7)
    {
        ArgumentNullException.ThrowIfNull(examples);
        if (string.IsNullOrWhiteSpace(version))
        {
            throw new ArgumentException("Model version is required.", nameof(version));
        }

        var split = FraudDatasetSplitter.Split(examples, trainFraction);
        var model = LogisticRegressionFraudModel.Fit(split.Train);
        var probabilities = split.Test.Select(example => model.PredictProbability(example.Features)).ToArray();
        var metrics = FraudModelMetrics.Evaluate(split.Test, probabilities);
        return new TrainedFraudModel(model, metrics, version);
    }
}
