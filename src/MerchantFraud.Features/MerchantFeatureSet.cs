using MerchantFraud.Core.Domain;

namespace MerchantFraud.Features;

public sealed record MerchantFeatureSet
{
    public MerchantFeatureSet(string merchantId, string currency, DateTimeOffset asOf)
    {
        if (string.IsNullOrWhiteSpace(merchantId))
        {
            throw new ArgumentException("Merchant id is required.", nameof(merchantId));
        }

        var normalizedCurrency = currency.Trim().ToUpperInvariant();
        Money.Zero(normalizedCurrency);

        MerchantId = merchantId.Trim();
        Currency = normalizedCurrency;
        AsOf = asOf;
    }

    public string MerchantId { get; init; }

    public string Currency { get; init; }

    public DateTimeOffset AsOf { get; init; }

    public DateTimeOffset? FirstTransactionAt { get; init; }

    public DateTimeOffset? LastTransactionAt { get; init; }

    public int TransactionCount { get; init; }

    public int DeclinedCount { get; init; }

    public int RefundedCount { get; init; }

    public int ChargedBackCount { get; init; }

    public decimal DeclineRate { get; init; }

    public decimal TotalVolume { get; init; }

    public decimal AverageTicket { get; init; }

    public decimal MaxTicket { get; init; }

    public int RoundAmountCount { get; init; }

    public decimal RoundAmountShare { get; init; }

    public decimal MaxTicketToAverageRatio { get; init; }

    public decimal RefundVolume { get; init; }

    public decimal RefundRate { get; init; }

    public decimal RefundToVolumeRatio { get; init; }

    public int DisputeCount { get; init; }

    public int OpenDisputeCount { get; init; }

    public decimal DisputeVolume { get; init; }

    public decimal DisputeRate { get; init; }

    public decimal DisputeToVolumeRatio { get; init; }

    public decimal CardPresentShare { get; init; }

    public decimal CardNotPresentShare { get; init; }

    public decimal EcommerceShare { get; init; }

    public decimal MotoShare { get; init; }

    public decimal UnknownChannelShare { get; init; }

    public int TransactionsLast24Hours { get; init; }

    public int TransactionsLast7Days { get; init; }

    public int TransactionsLast30Days { get; init; }

    public decimal VolumeLast24Hours { get; init; }

    public decimal VolumeLast7Days { get; init; }

    public decimal VolumeLast30Days { get; init; }
}
