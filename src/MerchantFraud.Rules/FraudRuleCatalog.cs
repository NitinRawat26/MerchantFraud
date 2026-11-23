using MerchantFraud.Features;

namespace MerchantFraud.Rules;

public static class FraudRuleCatalog
{
    public static IReadOnlyList<FraudRule> Default { get; } = new[]
    {
        Rule(
            "DISPUTE_RATE_HIGH",
            "High dispute rate",
            "Merchant dispute rate is elevated.",
            FraudRuleSeverity.High,
            FraudRuleAction.Review,
            features => features.DisputeRate >= 0.03m,
            "Dispute rate is at or above 3%."),
        Rule(
            "DISPUTE_RATE_CRITICAL",
            "Critical dispute rate",
            "Merchant dispute rate is critical.",
            FraudRuleSeverity.Critical,
            FraudRuleAction.Decline,
            features => features.DisputeRate >= 0.10m,
            "Dispute rate is at or above 10%."),
        Rule(
            "DISPUTE_VOLUME_HIGH",
            "High dispute volume ratio",
            "Dispute volume is high relative to processed volume.",
            FraudRuleSeverity.High,
            FraudRuleAction.Review,
            features => features.DisputeToVolumeRatio >= 0.05m,
            "Dispute volume is at or above 5% of processed volume."),
        Rule(
            "REFUND_RATE_HIGH",
            "High refund rate",
            "Merchant refund rate is elevated.",
            FraudRuleSeverity.Medium,
            FraudRuleAction.Review,
            features => features.RefundRate >= 0.10m,
            "Refund rate is at or above 10%."),
        Rule(
            "REFUND_RATE_CRITICAL",
            "Critical refund rate",
            "Merchant refund rate is critical.",
            FraudRuleSeverity.Critical,
            FraudRuleAction.Decline,
            features => features.RefundRate >= 0.30m,
            "Refund rate is at or above 30%."),
        Rule(
            "DECLINE_RATE_HIGH",
            "High decline rate",
            "Merchant decline rate may indicate card testing.",
            FraudRuleSeverity.Medium,
            FraudRuleAction.Review,
            features => features.TransactionCount >= 20 && features.DeclineRate >= 0.50m,
            "At least half of transactions are declined across 20 or more transactions."),
        Rule(
            "CNP_DOMINANT",
            "Card-not-present dominant",
            "Merchant activity is overwhelmingly card-not-present.",
            FraudRuleSeverity.Medium,
            FraudRuleAction.Review,
            features => features.TransactionCount >= 20 && features.CardNotPresentShare >= 0.90m,
            "At least 90% of transactions are card-not-present across 20 or more transactions."),
        Rule(
            "VELOCITY_24H_HIGH",
            "High 24-hour velocity",
            "Merchant transaction velocity is elevated.",
            FraudRuleSeverity.High,
            FraudRuleAction.Review,
            features => features.TransactionsLast24Hours >= 100,
            "At least 100 transactions occurred in the last 24 hours."),
        Rule(
            "ROUND_AMOUNT_CONCENTRATION",
            "Round amount concentration",
            "Merchant volume is concentrated in round amounts.",
            FraudRuleSeverity.Medium,
            FraudRuleAction.Review,
            features => features.TransactionCount >= 20 && features.RoundAmountShare >= 0.80m,
            "At least 80% of volume-eligible transactions are round amounts across 20 or more transactions."),
        Rule(
            "TICKET_OUTLIER",
            "Ticket size outlier",
            "Maximum ticket is a large outlier versus average ticket.",
            FraudRuleSeverity.Medium,
            FraudRuleAction.Review,
            features => features.TransactionCount >= 10 && features.MaxTicketToAverageRatio >= 10m,
            "Maximum ticket is at least 10x average ticket across 10 or more transactions.")
    };

    private static FraudRule Rule(
        string id,
        string name,
        string description,
        FraudRuleSeverity severity,
        FraudRuleAction action,
        Func<MerchantFeatureSet, bool> condition,
        string message) =>
        new(id, name, description, severity, action, id, condition, message);
}
