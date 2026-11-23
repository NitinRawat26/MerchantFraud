using MerchantFraud.Features;
using MerchantFraud.Rules;

namespace MerchantFraud.Tests.Rules;

public class FraudRuleEngineTests
{
    private static readonly DateTimeOffset AsOf = new(2026, 1, 10, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Evaluate_LowRiskFeatures_ReturnsNoHits()
    {
        var features = Features(transactionCount: 10, cardNotPresentShare: 0.50m, roundAmountShare: 0.20m, maxTicketToAverageRatio: 2m);

        var evaluation = FraudRuleEngine.Evaluate(features);

        Assert.False(evaluation.HasHits);
        Assert.Empty(evaluation.Hits);
        Assert.Equal(FraudRuleAction.None, evaluation.RecommendedAction);
        Assert.Equal(FraudRuleSeverity.Low, evaluation.HighestSeverity);
    }

    [Fact]
    public void Evaluate_CriticalDisputeRate_ReturnsDecline()
    {
        var features = Features(transactionCount: 100, disputeRate: 0.12m, disputeToVolumeRatio: 0.02m);

        var evaluation = FraudRuleEngine.Evaluate(features);

        var ruleIds = evaluation.Hits.Select(hit => hit.RuleId).ToArray();
        Assert.Contains("DISPUTE_RATE_HIGH", ruleIds);
        Assert.Contains("DISPUTE_RATE_CRITICAL", ruleIds);
        Assert.DoesNotContain("DISPUTE_VOLUME_HIGH", ruleIds);
        Assert.Equal(FraudRuleAction.Decline, evaluation.RecommendedAction);
        Assert.Equal(FraudRuleSeverity.Critical, evaluation.HighestSeverity);

        var critical = evaluation.Hits.Single(hit => hit.RuleId == "DISPUTE_RATE_CRITICAL");
        Assert.Equal("DISPUTE_RATE_CRITICAL", critical.ReasonCode);
        Assert.Equal(FraudRuleAction.Decline, critical.Action);
    }

    [Fact]
    public void Evaluate_RefundAndCnpRisks_ReturnReviewWithoutDecline()
    {
        var features = Features(transactionCount: 50, refundRate: 0.12m, cardNotPresentShare: 0.95m, declineRate: 0.10m);

        var evaluation = FraudRuleEngine.Evaluate(features);

        var ruleIds = evaluation.Hits.Select(hit => hit.RuleId).ToArray();
        Assert.Contains("REFUND_RATE_HIGH", ruleIds);
        Assert.Contains("CNP_DOMINANT", ruleIds);
        Assert.DoesNotContain("REFUND_RATE_CRITICAL", ruleIds);
        Assert.Equal(FraudRuleAction.Review, evaluation.RecommendedAction);
        Assert.Equal(FraudRuleSeverity.Medium, evaluation.HighestSeverity);
    }

    [Fact]
    public void Evaluate_VelocityRoundAmountsAndTicketOutlier_ReturnReview()
    {
        var features = Features(
            transactionCount: 30,
            transactionsLast24Hours: 120,
            roundAmountShare: 0.90m,
            maxTicketToAverageRatio: 12m);

        var evaluation = FraudRuleEngine.Evaluate(features);

        var ruleIds = evaluation.Hits.Select(hit => hit.RuleId).ToArray();
        Assert.Contains("VELOCITY_24H_HIGH", ruleIds);
        Assert.Contains("ROUND_AMOUNT_CONCENTRATION", ruleIds);
        Assert.Contains("TICKET_OUTLIER", ruleIds);
        Assert.Equal(FraudRuleAction.Review, evaluation.RecommendedAction);
        Assert.Equal(FraudRuleSeverity.High, evaluation.HighestSeverity);
    }

    [Fact]
    public void Evaluate_CustomRules_CanReplaceCatalog()
    {
        var customRules = new[]
        {
            new FraudRule(
                "CUSTOM_HARD_STOP",
                "Custom hard stop",
                "Custom rule for tests.",
                FraudRuleSeverity.Critical,
                FraudRuleAction.Decline,
                "CUSTOM_HARD_STOP_CODE",
                features => features.MerchantId == "MER-1",
                "Custom hard stop hit.")
        };

        var evaluation = FraudRuleEngine.Evaluate(Features(), customRules);

        var hit = Assert.Single(evaluation.Hits);
        Assert.Equal("CUSTOM_HARD_STOP", hit.RuleId);
        Assert.Equal("CUSTOM_HARD_STOP_CODE", hit.ReasonCode);
        Assert.Equal(FraudRuleAction.Decline, evaluation.RecommendedAction);
    }

    [Fact]
    public void Evaluate_EmptyRuleSet_ReturnsNoHits()
    {
        var evaluation = FraudRuleEngine.Evaluate(Features(disputeRate: 1m), Array.Empty<FraudRule>());

        Assert.False(evaluation.HasHits);
        Assert.Equal(FraudRuleAction.None, evaluation.RecommendedAction);
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
