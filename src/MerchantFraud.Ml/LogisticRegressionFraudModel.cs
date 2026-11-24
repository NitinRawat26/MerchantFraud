namespace MerchantFraud.Ml;

public interface IFraudModel
{
    string ModelVersion { get; }

    double PredictProbability(IReadOnlyList<double> features);
}

public sealed class LogisticRegressionFraudModel : IFraudModel
{
    private readonly double[] _weights;
    private readonly double[] _means;
    private readonly double[] _stds;
    private readonly double _bias;

    private LogisticRegressionFraudModel(double[] weights, double[] means, double[] stds, double bias)
    {
        _weights = weights;
        _means = means;
        _stds = stds;
        _bias = bias;
    }

    public string ModelVersion => "logreg-v1";

    public static LogisticRegressionFraudModel Fit(IReadOnlyList<FraudTrainingExample> examples, double learningRate = 0.5, int epochs = 400)
    {
        ArgumentNullException.ThrowIfNull(examples);
        if (examples.Count < 2)
        {
            throw new ArgumentException("At least two examples are required to fit a model.", nameof(examples));
        }

        if (learningRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(learningRate), learningRate, "Learning rate must be positive.");
        }

        if (epochs < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(epochs), epochs, "Epochs must be at least 1.");
        }

        var featureCount = examples[0].Features.Count;
        if (featureCount == 0)
        {
            throw new ArgumentException("Examples must contain features.", nameof(examples));
        }

        foreach (var example in examples)
        {
            if (example.Features.Count != featureCount)
            {
                throw new ArgumentException("All examples must have the same feature count.", nameof(examples));
            }
        }

        var means = new double[featureCount];
        var stds = new double[featureCount];
        ComputeNormalization(examples, means, stds);

        var standardized = examples
            .Select(example => example.Features.Select((value, index) => (value - means[index]) / stds[index]).ToArray())
            .ToArray();
        var labels = examples.Select(example => example.IsFraudulent ? 1.0 : 0.0).ToArray();
        var weights = new double[featureCount];
        var bias = InitialBias(labels);

        for (var epoch = 0; epoch < epochs; epoch++)
        {
            for (var exampleIndex = 0; exampleIndex < standardized.Length; exampleIndex++)
            {
                var features = standardized[exampleIndex];
                var z = bias;
                for (var featureIndex = 0; featureIndex < featureCount; featureIndex++)
                {
                    z += weights[featureIndex] * features[featureIndex];
                }

                var error = Sigmoid(z) - labels[exampleIndex];
                for (var featureIndex = 0; featureIndex < featureCount; featureIndex++)
                {
                    weights[featureIndex] -= learningRate * error * features[featureIndex];
                }

                bias -= learningRate * error;
            }
        }

        return new LogisticRegressionFraudModel(weights, means, stds, bias);
    }

    public double PredictProbability(IReadOnlyList<double> features)
    {
        ArgumentNullException.ThrowIfNull(features);
        if (features.Count != _weights.Length)
        {
            throw new ArgumentException("Feature count does not match the trained model.", nameof(features));
        }

        var z = _bias;
        for (var index = 0; index < _weights.Length; index++)
        {
            z += _weights[index] * ((features[index] - _means[index]) / _stds[index]);
        }

        return Sigmoid(z);
    }

    private static void ComputeNormalization(IReadOnlyList<FraudTrainingExample> examples, double[] means, double[] stds)
    {
        var count = examples.Count;
        foreach (var example in examples)
        {
            for (var index = 0; index < means.Length; index++)
            {
                means[index] += example.Features[index];
            }
        }

        for (var index = 0; index < means.Length; index++)
        {
            means[index] /= count;
        }

        foreach (var example in examples)
        {
            for (var index = 0; index < stds.Length; index++)
            {
                var delta = example.Features[index] - means[index];
                stds[index] += delta * delta;
            }
        }

        for (var index = 0; index < stds.Length; index++)
        {
            stds[index] = Math.Sqrt(stds[index] / count);
            if (stds[index] < 1e-9)
            {
                stds[index] = 1;
            }
        }
    }

    private static double InitialBias(double[] labels)
    {
        var positives = labels.Count(label => label > 0.5);
        var negatives = labels.Length - positives;
        if (positives == 0 || negatives == 0)
        {
            return 0;
        }

        return Math.Clamp(Math.Log((double)positives / negatives), -3, 3);
    }

    private static double Sigmoid(double z)
    {
        if (z >= 0)
        {
            var exp = Math.Exp(-z);
            return 1 / (1 + exp);
        }

        var expNegative = Math.Exp(z);
        return expNegative / (1 + expNegative);
    }
}
