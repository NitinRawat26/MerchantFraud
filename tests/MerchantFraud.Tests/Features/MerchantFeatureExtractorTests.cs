using MerchantFraud.Core.Domain;
using MerchantFraud.Features;

namespace MerchantFraud.Tests.Features;

public class MerchantFeatureExtractorTests
{
    private static readonly DateTimeOffset AsOf = new(2026, 1, 10, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Extract_EmptyMerchant_ReturnsZeroFeatures()
    {
        var features = MerchantFeatureExtractor.Extract("MER-1", Array.Empty<FraudTransaction>(), currency: "usd", asOf: AsOf);

        Assert.Equal("MER-1", features.MerchantId);
        Assert.Equal("USD", features.Currency);
        Assert.Equal(AsOf, features.AsOf);
        Assert.Equal(0, features.TransactionCount);
        Assert.Equal(0m, features.TotalVolume);
        Assert.Null(features.FirstTransactionAt);
        Assert.Null(features.LastTransactionAt);
    }

    [Fact]
    public void Extract_RequiresCurrencyWhenMerchantHasNoActivity()
    {
        Assert.Throws<ArgumentException>(() => MerchantFeatureExtractor.Extract("MER-1", Array.Empty<FraudTransaction>()));
    }

    [Fact]
    public void Extract_CalculatesActivityAmountRefundDisputeChannelAndWindowFeatures()
    {
        var transactions = new[]
        {
            Transaction("T1", "MER-1", new DateTimeOffset(2026, 1, 9, 12, 0, 0, TimeSpan.Zero), 100m, TransactionChannel.CardPresent, TransactionStatus.Captured),
            Transaction("T2", "MER-1", new DateTimeOffset(2026, 1, 9, 13, 0, 0, TimeSpan.Zero), 200m, TransactionChannel.CardNotPresent, TransactionStatus.Refunded),
            Transaction("T3", "MER-1", new DateTimeOffset(2026, 1, 8, 12, 0, 0, TimeSpan.Zero), 50m, TransactionChannel.Ecommerce, TransactionStatus.Declined),
            Transaction("T4", "MER-1", new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero), 300m, TransactionChannel.Moto, TransactionStatus.Settled),
            Transaction("T9", "MER-2", new DateTimeOffset(2026, 1, 9, 12, 0, 0, TimeSpan.Zero), 999m, TransactionChannel.CardPresent, TransactionStatus.Captured)
        };
        var disputes = new[]
        {
            Dispute("D1", "T1", "MER-1", new DateTimeOffset(2026, 1, 9, 15, 0, 0, TimeSpan.Zero), 100m, DisputeStatus.Open),
            Dispute("D2", "T2", "MER-1", new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.Zero), 50m, DisputeStatus.Won),
            Dispute("D9", "T9", "MER-2", new DateTimeOffset(2026, 1, 9, 15, 0, 0, TimeSpan.Zero), 999m, DisputeStatus.Open)
        };

        var features = MerchantFeatureExtractor.Extract("MER-1", transactions, disputes, asOf: AsOf);

        Assert.Equal("USD", features.Currency);
        Assert.Equal(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero), features.FirstTransactionAt);
        Assert.Equal(new DateTimeOffset(2026, 1, 9, 13, 0, 0, TimeSpan.Zero), features.LastTransactionAt);
        Assert.Equal(4, features.TransactionCount);
        Assert.Equal(1, features.DeclinedCount);
        Assert.Equal(1, features.RefundedCount);
        Assert.Equal(0.25m, features.DeclineRate);
        Assert.Equal(0.25m, features.RefundRate);

        Assert.Equal(600m, features.TotalVolume);
        Assert.Equal(200m, features.AverageTicket);
        Assert.Equal(300m, features.MaxTicket);
        Assert.Equal(3, features.RoundAmountCount);
        Assert.Equal(1m, features.RoundAmountShare);
        Assert.Equal(1.5m, features.MaxTicketToAverageRatio);

        Assert.Equal(200m, features.RefundVolume);
        Assert.Equal(0.333333m, features.RefundToVolumeRatio);
        Assert.Equal(2, features.DisputeCount);
        Assert.Equal(1, features.OpenDisputeCount);
        Assert.Equal(150m, features.DisputeVolume);
        Assert.Equal(0.5m, features.DisputeRate);
        Assert.Equal(0.25m, features.DisputeToVolumeRatio);

        Assert.Equal(0.25m, features.CardPresentShare);
        Assert.Equal(0.25m, features.CardNotPresentShare);
        Assert.Equal(0.25m, features.EcommerceShare);
        Assert.Equal(0.25m, features.MotoShare);
        Assert.Equal(0m, features.UnknownChannelShare);

        Assert.Equal(2, features.TransactionsLast24Hours);
        Assert.Equal(3, features.TransactionsLast7Days);
        Assert.Equal(4, features.TransactionsLast30Days);
        Assert.Equal(300m, features.VolumeLast24Hours);
        Assert.Equal(300m, features.VolumeLast7Days);
        Assert.Equal(600m, features.VolumeLast30Days);
    }

    [Fact]
    public void Extract_AsOfProvidesPointInTimeFeatures()
    {
        var asOf = new DateTimeOffset(2026, 1, 9, 12, 30, 0, TimeSpan.Zero);
        var transactions = new[]
        {
            Transaction("T1", "MER-1", new DateTimeOffset(2026, 1, 9, 12, 0, 0, TimeSpan.Zero), 100m, TransactionChannel.CardPresent, TransactionStatus.Captured),
            Transaction("T2", "MER-1", new DateTimeOffset(2026, 1, 9, 13, 0, 0, TimeSpan.Zero), 200m, TransactionChannel.CardNotPresent, TransactionStatus.Captured),
            Transaction("T3", "MER-1", new DateTimeOffset(2026, 1, 8, 12, 0, 0, TimeSpan.Zero), 50m, TransactionChannel.Ecommerce, TransactionStatus.Declined)
        };
        var disputes = new[]
        {
            Dispute("D1", "T1", "MER-1", new DateTimeOffset(2026, 1, 9, 15, 0, 0, TimeSpan.Zero), 100m, DisputeStatus.Open),
            Dispute("D2", "T3", "MER-1", new DateTimeOffset(2026, 1, 8, 13, 0, 0, TimeSpan.Zero), 50m, DisputeStatus.Won)
        };

        var features = MerchantFeatureExtractor.Extract("MER-1", transactions, disputes, asOf: asOf);

        Assert.Equal(2, features.TransactionCount);
        Assert.Equal(100m, features.TotalVolume);
        Assert.Equal(1, features.DisputeCount);
        Assert.Equal(1, features.TransactionsLast24Hours);
        Assert.Equal(100m, features.VolumeLast24Hours);
    }

    [Fact]
    public void Extract_RejectsMixedCurrencies()
    {
        var transactions = new[]
        {
            Transaction("T1", "MER-1", AsOf, 100m),
            new FraudTransaction("T2", "MER-1", AsOf, new Money(100m, "EUR"))
        };

        Assert.Throws<ArgumentException>(() => MerchantFeatureExtractor.Extract("MER-1", transactions));
    }

    [Fact]
    public void Extract_RejectsRequestedCurrencyMismatch()
    {
        var transactions = new[] { Transaction("T1", "MER-1", AsOf, 100m) };

        Assert.Throws<ArgumentException>(() => MerchantFeatureExtractor.Extract("MER-1", transactions, currency: "EUR"));
    }

    private static FraudTransaction Transaction(
        string id,
        string merchantId,
        DateTimeOffset occurredAt,
        decimal amount,
        TransactionChannel channel = TransactionChannel.CardPresent,
        TransactionStatus status = TransactionStatus.Captured) =>
        new(id, merchantId, occurredAt, new Money(amount, "USD"), channel, status);

    private static FraudDispute Dispute(
        string id,
        string transactionId,
        string merchantId,
        DateTimeOffset openedAt,
        decimal amount,
        DisputeStatus status = DisputeStatus.Open) =>
        new(id, transactionId, merchantId, openedAt, new Money(amount, "USD"), "fraud", status);
}
