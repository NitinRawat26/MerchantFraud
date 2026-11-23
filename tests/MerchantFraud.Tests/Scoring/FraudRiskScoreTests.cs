using MerchantFraud.Core.Scoring;

namespace MerchantFraud.Tests.Scoring;

public class FraudRiskScoreTests
{
    [Theory]
    [InlineData(0, FraudRiskBand.Low)]
    [InlineData(249, FraudRiskBand.Low)]
    [InlineData(250, FraudRiskBand.Medium)]
    [InlineData(499, FraudRiskBand.Medium)]
    [InlineData(500, FraudRiskBand.High)]
    [InlineData(749, FraudRiskBand.High)]
    [InlineData(750, FraudRiskBand.Critical)]
    [InlineData(1000, FraudRiskBand.Critical)]
    public void Band_MatchesScoreThresholds(int value, FraudRiskBand expected)
    {
        var score = new FraudRiskScore(value);

        Assert.Equal(expected, score.Band);
    }

    [Fact]
    public void ReasonCodes_AreTrimmedDistinctAndCaseInsensitive()
    {
        var score = new FraudRiskScore(800, new[] { " VELOCITY_SPIKE ", "velocity_spike", "", "MANUAL_REVIEW" });

        Assert.Equal(new[] { "VELOCITY_SPIKE", "MANUAL_REVIEW" }, score.ReasonCodes);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1001)]
    public void Constructor_RejectsScoresOutsideRange(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FraudRiskScore(value));
    }
}
