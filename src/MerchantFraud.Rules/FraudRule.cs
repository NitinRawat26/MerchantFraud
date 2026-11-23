using MerchantFraud.Features;

namespace MerchantFraud.Rules;

public enum FraudRuleSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum FraudRuleAction
{
    None,
    Monitor,
    Review,
    Decline
}

public sealed record FraudRule(
    string Id,
    string Name,
    string Description,
    FraudRuleSeverity Severity,
    FraudRuleAction Action,
    string ReasonCode,
    Func<MerchantFeatureSet, bool> Condition,
    string Message);

public sealed record FraudRuleHit(
    string RuleId,
    string RuleName,
    FraudRuleSeverity Severity,
    FraudRuleAction Action,
    string ReasonCode,
    string Message);

public sealed record FraudRuleEvaluation(
    string MerchantId,
    IReadOnlyList<FraudRuleHit> Hits,
    FraudRuleAction RecommendedAction,
    FraudRuleSeverity HighestSeverity)
{
    public bool HasHits => Hits.Count > 0;
}
