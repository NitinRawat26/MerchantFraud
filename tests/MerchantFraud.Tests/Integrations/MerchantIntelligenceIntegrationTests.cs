using MerchantFraud.Core.Domain;
using MerchantFraud.Features;
using MerchantFraud.Integrations;
using MerchantFraud.Scoring;

namespace MerchantFraud.Tests.Integrations;

public class MerchantIntelligenceIntegrationTests
{
    [Fact]
    public void SignalMapper_MapsDeclineHighRiskAndReasonCodes()
    {
        var assessment = new MerchantIntelligenceAssessment(
            "MER-1",
            "ASM-1",
            MerchantIntelligenceDecision.Decline,
            820,
            new[] { "sanctions", "high-risk-mcc" },
            new DateTimeOffset(2026, 1, 20, 12, 0, 0, TimeSpan.Zero));

        var signals = MerchantIntelligenceSignalMapper.Map(assessment);

        Assert.Contains(signals, signal => signal.Code == "MI_DECISION_DECLINE" && signal.Points == 250);
        Assert.Contains(signals, signal => signal.Code == "MI_RISK_HIGH" && signal.Points == 150);
        Assert.Contains(signals, signal => signal.Code == "MI_REASON_CODES" && signal.Points == 20);
    }

    [Fact]
    public void ScoringEngine_IncludesMerchantIntelligenceSignals()
    {
        var features = new MerchantFeatureSet("MER-1", "USD", new DateTimeOffset(2026, 1, 31, 12, 0, 0, TimeSpan.Zero));
        var signals = MerchantIntelligenceSignalMapper.Map(new MerchantIntelligenceAssessment(
            "MER-1",
            "ASM-1",
            MerchantIntelligenceDecision.Refer,
            520,
            new[] { "stale-website" },
            new DateTimeOffset(2026, 1, 20, 12, 0, 0, TimeSpan.Zero)));

        var result = MerchantScoringEngine.Score(features, additionalSignals: signals);

        Assert.Equal(210, result.ScoreResult.Score.Value);
        Assert.Contains("MI_DECISION_REFER", result.ScoreResult.Score.ReasonCodes);
        Assert.Contains("MI_RISK_MEDIUM", result.ScoreResult.Score.ReasonCodes);
        Assert.Contains(result.Signals, signal => signal.Code == "MI_DECISION_REFER");
    }
}
