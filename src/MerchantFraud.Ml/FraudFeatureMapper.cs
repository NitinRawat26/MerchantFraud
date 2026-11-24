using MerchantFraud.Features;

namespace MerchantFraud.Ml;

public static class FraudFeatureMapper
{
    public static IReadOnlyList<string> FeatureNames { get; } = new[]
    {
        "transaction_count_log",
        "decline_rate",
        "refund_rate",
        "refund_to_volume_ratio",
        "dispute_rate",
        "dispute_to_volume_ratio",
        "card_not_present_share",
        "ecommerce_share",
        "round_amount_share",
        "max_ticket_to_average_ratio",
        "average_ticket_scaled",
        "volume_last7_days_log",
        "transactions_last7_days_log",
        "transactions_last24_hours_log",
        "volume_last30_days_log"
    };

    public static int FeatureCount => FeatureNames.Count;

    public static double[] Map(MerchantFeatureSet features)
    {
        ArgumentNullException.ThrowIfNull(features);

        return new[]
        {
            Log1p(features.TransactionCount),
            (double)features.DeclineRate,
            (double)features.RefundRate,
            (double)features.RefundToVolumeRatio,
            (double)features.DisputeRate,
            (double)features.DisputeToVolumeRatio,
            (double)features.CardNotPresentShare,
            (double)features.EcommerceShare,
            (double)features.RoundAmountShare,
            (double)features.MaxTicketToAverageRatio,
            (double)features.AverageTicket / 1000.0,
            Log1p((double)features.VolumeLast7Days),
            Log1p(features.TransactionsLast7Days),
            Log1p(features.TransactionsLast24Hours),
            Log1p((double)features.VolumeLast30Days)
        };
    }

    private static double Log1p(double value) => Math.Log(1 + Math.Max(0, value));
}
