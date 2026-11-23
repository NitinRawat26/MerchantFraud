namespace MerchantFraud.Core.Scoring;

public enum FraudRiskBand
{
    Low,
    Medium,
    High,
    Critical
}

public static class FraudRiskBands
{
    public const int MediumThreshold = 250;
    public const int HighThreshold = 500;
    public const int CriticalThreshold = 750;

    public static FraudRiskBand For(int score) => score switch
    {
        < MediumThreshold => FraudRiskBand.Low,
        < HighThreshold => FraudRiskBand.Medium,
        < CriticalThreshold => FraudRiskBand.High,
        _ => FraudRiskBand.Critical
    };
}

public sealed class FraudRiskScore
{
    public FraudRiskScore(int value, IEnumerable<string>? reasonCodes = null)
    {
        if (value is < 0 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Fraud risk score must be between 0 and 1000.");
        }

        Value = value;
        ReasonCodes = (reasonCodes ?? Enumerable.Empty<string>())
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public int Value { get; }

    public IReadOnlyList<string> ReasonCodes { get; }

    public FraudRiskBand Band => FraudRiskBands.For(Value);
}
