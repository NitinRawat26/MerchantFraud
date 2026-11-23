using MerchantFraud.Core.Domain;
using MerchantFraud.Core.Scoring;
using MerchantFraud.Features;
using MerchantFraud.Rules;
using MerchantFraud.Scoring;

namespace MerchantFraud.Tests.Scoring;

public class MerchantScoringEngineTests
{
    private static readonly DateTimeOffset AsOf = new(2026, 1, 10, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Score_LowRiskFeatures_ReturnsLowScoreWithoutSignals()
    {
        var features = Features(transactionCount: 10, cardNotPresentShare: 0.50m, roundAmountShare: 0.20m, maxTicketToAverageRatio: 2m);

        var result = MerchantScoringEngine.Score(features, scoredAt: AsOf);

        Assert.Equal(0, result.ScoreResult.Score.Value);
        Assert.Equal(FraudRiskBand.Low, result.ScoreResult.Score.Band);
        Assert.Empty(result.ScoreResult.Score.ReasonCodes);
        Assert.Empty(result.Signals);
        Assert.Equal(0, result.RawScore);
        Assert.Equal(FraudScoreSubjectType.Merchant, result.ScoreResult.SubjectType);
        Assert.Equal("MER-1", result.ScoreResult.SubjectId);
        Assert.Equal("rules-v1", result.ScoreResult.ModelVersion);
        Assert.Equal(AsOf, result.ScoreResult.ScoredAt);
    }

    [Fact]
    public void Score_ElevatedRisk_ReturnsCriticalScoreWithReasonCodes()
    {
        var features = Features(
            transactionCount: 100,
            disputeRate: 0.12m,
            disputeToVolumeRatio: 0.02m,
            refundRate: 0.12m,
            declineRate: 0.10m,
            cardNotPresentShare: 0.95m,
            transactionsLast24Hours: 120,
            roundAmountShare: 0.90m,
            maxTicketToAverageRatio: 12m);

        var result = MerchantScoringEngine.Score(features, scoredAt: AsOf);

        Assert.True(result.ScoreResult.Score.Value >= FraudRiskBands.CriticalThreshold);
        Assert.Equal(FraudRiskBand.Critical, result.ScoreResult.Score.Band);
        Assert.Equal(result.RawScore, result.ScoreResult.Score.Value);
        Assert.Contains("DISPUTE_RATE_CRITICAL", result.ScoreResult.Score.ReasonCodes);
        Assert.Contains("FEATURE_DISPUTE_RATE", result.ScoreResult.Score.ReasonCodes);
        Assert.Contains("FEATURE_VELOCITY_24H", result.ScoreResult.Score.ReasonCodes);
        Assert.NotEmpty(result.Signals);
        Assert.Equal(FraudRuleAction.Decline, result.RuleEvaluation.RecommendedAction);
    }

    [Fact]
    public void Score_VeryHighRisk_CapsAt1000()
    {
        var features = Features(
            transactionCount: 200,
            disputeRate: 0.20m,
            disputeToVolumeRatio: 0.10m,
            refundRate: 0.35m,
            declineRate: 0.60m,
            cardNotPresentShare: 1.0m,
            transactionsLast24Hours: 300,
            roundAmountShare: 1.0m,
            maxTicketToAverageRatio: 20m);

        var result = MerchantScoringEngine.Score(features, scoredAt: AsOf);

        Assert.Equal(1000, result.ScoreResult.Score.Value);
        Assert.Equal(FraudRiskBand.Critical, result.ScoreResult.Score.Band);
        Assert.True(result.RawScore > result.ScoreResult.Score.Value);
    }

    [Fact]
    public void Score_UsesProvidedModelVersion()
    {
        var features = Features(transactionCount: 1);

        var result = MerchantScoringEngine.Score(features, modelVersion: "test-v1", scoredAt: AsOf);

        Assert.Equal("test-v1", result.ScoreResult.ModelVersion);
        Assert.StartsWith("score-MER-1-", result.ScoreResult.Id, StringComparison.Ordinal);
    }

    [Fact]
    public void Score_RejectsMismatchedRuleEvaluation()
    {
        var features = Features();
        var otherFeatures = new MerchantFeatureSet("MER-2", "USD", AsOf);
        var mismatchedEvaluation = FraudRuleEngine.Evaluate(otherFeatures);

        Assert.Throws<ArgumentException>(() => MerchantScoringEngine.Score(features, mismatchedEvaluation));
    }

    private static MerchantFeatureSet Features(
        int transactionCount = 0,
        decimal disputeRate = 0m,
        decimal disputeToVolumeRatio = 0m,
        decimal refundRate = 0m,
        decimal declineRate = 0m,
        decimal cardNotPresentShare = 0m,
        int transactionsLast24Hours = 0,
        decimal roundAmountShare = 0m,
        decimal maxTicketToAverageRatio = 0m) =>
        new("MER-1", "USD", AsOf)
        {
            TransactionCount = transactionCount,
            DisputeRate = disputeRate,
            DisputeToVolumeRatio = disputeToVolumeRatio,
            RefundRate = refundRate,
            DeclineRate = declineRate,
            CardNotPresentShare = cardNotPresentShare,
            TransactionsLast24Hours = transactionsLast24Hours,
            RoundAmountShare = roundAmountShare,
            MaxTicketToAverageRatio = maxTicketToAverageRatio
        };
}
