using MerchantFraud.Features;

namespace MerchantFraud.Rules;

public static class FraudRuleEngine
{
    public static FraudRuleEvaluation Evaluate(MerchantFeatureSet features, IEnumerable<FraudRule>? rules = null)
    {
        ArgumentNullException.ThrowIfNull(features);

        var ruleSet = rules ?? FraudRuleCatalog.Default;
        var hits = ruleSet
            .Where(rule => rule.Condition(features))
            .Select(rule => new FraudRuleHit(rule.Id, rule.Name, rule.Severity, rule.Action, rule.ReasonCode, rule.Message))
            .ToArray();

        var recommendedAction = hits.Length == 0
            ? FraudRuleAction.None
            : (FraudRuleAction)hits.Max(hit => (int)hit.Action);
        var highestSeverity = hits.Length == 0
            ? FraudRuleSeverity.Low
            : (FraudRuleSeverity)hits.Max(hit => (int)hit.Severity);

        return new FraudRuleEvaluation(features.MerchantId, hits, recommendedAction, highestSeverity);
    }
}
