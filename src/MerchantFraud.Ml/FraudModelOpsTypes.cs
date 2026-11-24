namespace MerchantFraud.Ml;

public enum FraudModelStatus
{
    Champion,
    Challenger,
    Retired
}

public sealed record FraudModelVersion(
    string Version,
    string Name,
    FraudModelStatus Status,
    DateTimeOffset TrainedAt,
    FraudModelMetrics? Metrics);

public sealed record TrainedFraudModel(
    IFraudModel Model,
    FraudModelMetrics Metrics,
    string Version);

public sealed record FeatureDrift(
    string FeatureName,
    double BaselineMean,
    double CurrentMean,
    double MeanShift,
    bool IsDrifted);

public sealed record FraudDriftReport(
    IReadOnlyList<FeatureDrift> Features,
    bool HasSignificantDrift);
