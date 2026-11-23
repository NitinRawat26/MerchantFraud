using MerchantFraud.Core.Domain;

namespace MerchantFraud.Features;

public static class MerchantFeatureExtractor
{
    public static MerchantFeatureSet Extract(
        string merchantId,
        IEnumerable<FraudTransaction> transactions,
        IEnumerable<FraudDispute>? disputes = null,
        string? currency = null,
        DateTimeOffset? asOf = null)
    {
        ArgumentNullException.ThrowIfNull(transactions);
        if (string.IsNullOrWhiteSpace(merchantId))
        {
            throw new ArgumentException("Merchant id is required.", nameof(merchantId));
        }

        var normalizedMerchantId = merchantId.Trim();
        var merchantTransactions = transactions
            .Where(transaction => string.Equals(transaction.MerchantId, normalizedMerchantId, StringComparison.Ordinal))
            .ToList();
        var merchantDisputes = (disputes ?? Enumerable.Empty<FraudDispute>())
            .Where(dispute => string.Equals(dispute.MerchantId, normalizedMerchantId, StringComparison.Ordinal))
            .ToList();

        if (merchantTransactions.Count == 0 && merchantDisputes.Count == 0)
        {
            if (string.IsNullOrWhiteSpace(currency))
            {
                throw new ArgumentException("Currency is required when the merchant has no transactions or disputes.", nameof(currency));
            }

            return new MerchantFeatureSet(normalizedMerchantId, currency, asOf ?? DateTimeOffset.UtcNow);
        }

        var featureCurrency = ResolveCurrency(merchantTransactions, merchantDisputes, currency);
        var effectiveAsOf = asOf ?? merchantTransactions
            .Select(transaction => transaction.OccurredAt)
            .Concat(merchantDisputes.Select(dispute => dispute.OpenedAt))
            .Max();
        var windowTransactions = merchantTransactions.Where(transaction => transaction.OccurredAt <= effectiveAsOf).ToList();
        var windowDisputes = merchantDisputes.Where(dispute => dispute.OpenedAt <= effectiveAsOf).ToList();

        return Build(normalizedMerchantId, featureCurrency, effectiveAsOf, windowTransactions, windowDisputes);
    }

    private static MerchantFeatureSet Build(
        string merchantId,
        string currency,
        DateTimeOffset asOf,
        IReadOnlyList<FraudTransaction> transactions,
        IReadOnlyList<FraudDispute> disputes)
    {
        var transactionCount = transactions.Count;
        var volumeTransactions = transactions.Where(transaction => transaction.Status != TransactionStatus.Declined).ToList();
        var volumeTransactionCount = volumeTransactions.Count;
        var totalVolume = volumeTransactions.Sum(transaction => transaction.Amount.Amount);
        var averageTicket = volumeTransactionCount == 0 ? 0m : totalVolume / volumeTransactionCount;
        var maxTicket = volumeTransactionCount == 0 ? 0m : volumeTransactions.Max(transaction => transaction.Amount.Amount);
        var declinedCount = transactions.Count(transaction => transaction.Status == TransactionStatus.Declined);
        var refundedCount = transactions.Count(transaction => transaction.Status == TransactionStatus.Refunded);
        var chargedBackCount = transactions.Count(transaction => transaction.Status == TransactionStatus.ChargedBack);
        var refundVolume = transactions
            .Where(transaction => transaction.Status == TransactionStatus.Refunded)
            .Sum(transaction => transaction.Amount.Amount);
        var roundAmountCount = volumeTransactions.Count(transaction => IsRoundAmount(transaction.Amount.Amount));
        var disputeCount = disputes.Count;
        var openDisputeCount = disputes.Count(dispute => dispute.Status is DisputeStatus.Open or DisputeStatus.UnderReview);
        var disputeVolume = disputes.Sum(dispute => dispute.Amount.Amount);
        var cardPresentCount = transactions.Count(transaction => transaction.Channel == TransactionChannel.CardPresent);
        var cardNotPresentCount = transactions.Count(transaction => transaction.Channel == TransactionChannel.CardNotPresent);
        var ecommerceCount = transactions.Count(transaction => transaction.Channel == TransactionChannel.Ecommerce);
        var motoCount = transactions.Count(transaction => transaction.Channel == TransactionChannel.Moto);
        var unknownChannelCount = transactions.Count(transaction => transaction.Channel == TransactionChannel.Unknown);
        var last24Hours = WindowStats(transactions, asOf, TimeSpan.FromHours(24));
        var last7Days = WindowStats(transactions, asOf, TimeSpan.FromDays(7));
        var last30Days = WindowStats(transactions, asOf, TimeSpan.FromDays(30));

        return new MerchantFeatureSet(merchantId, currency, asOf)
        {
            FirstTransactionAt = transactionCount == 0 ? null : transactions.Min(transaction => transaction.OccurredAt),
            LastTransactionAt = transactionCount == 0 ? null : transactions.Max(transaction => transaction.OccurredAt),
            TransactionCount = transactionCount,
            DeclinedCount = declinedCount,
            RefundedCount = refundedCount,
            ChargedBackCount = chargedBackCount,
            DeclineRate = Ratio(declinedCount, transactionCount),
            TotalVolume = totalVolume,
            AverageTicket = averageTicket,
            MaxTicket = maxTicket,
            RoundAmountCount = roundAmountCount,
            RoundAmountShare = Ratio(roundAmountCount, volumeTransactionCount),
            MaxTicketToAverageRatio = averageTicket == 0m ? 0m : Math.Round(maxTicket / averageTicket, 6),
            RefundVolume = refundVolume,
            RefundRate = Ratio(refundedCount, transactionCount),
            RefundToVolumeRatio = Ratio(refundVolume, totalVolume),
            DisputeCount = disputeCount,
            OpenDisputeCount = openDisputeCount,
            DisputeVolume = disputeVolume,
            DisputeRate = Ratio(disputeCount, transactionCount),
            DisputeToVolumeRatio = Ratio(disputeVolume, totalVolume),
            CardPresentShare = Ratio(cardPresentCount, transactionCount),
            CardNotPresentShare = Ratio(cardNotPresentCount, transactionCount),
            EcommerceShare = Ratio(ecommerceCount, transactionCount),
            MotoShare = Ratio(motoCount, transactionCount),
            UnknownChannelShare = Ratio(unknownChannelCount, transactionCount),
            TransactionsLast24Hours = last24Hours.Count,
            TransactionsLast7Days = last7Days.Count,
            TransactionsLast30Days = last30Days.Count,
            VolumeLast24Hours = last24Hours.Volume,
            VolumeLast7Days = last7Days.Volume,
            VolumeLast30Days = last30Days.Volume
        };
    }

    private static string ResolveCurrency(
        IReadOnlyList<FraudTransaction> transactions,
        IReadOnlyList<FraudDispute> disputes,
        string? requestedCurrency)
    {
        var currencies = transactions
            .Select(transaction => transaction.Amount.Currency)
            .Concat(disputes.Select(dispute => dispute.Amount.Currency))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (currencies.Length > 1)
        {
            throw new ArgumentException("Merchant features require transactions and disputes in a single currency.");
        }

        var currency = currencies.Length == 1 ? currencies[0] : requestedCurrency;
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(requestedCurrency));
        }

        var normalized = currency.Trim().ToUpperInvariant();
        if (requestedCurrency is not null)
        {
            var requested = requestedCurrency.Trim().ToUpperInvariant();
            Money.Zero(requested);
            if (!string.Equals(requested, normalized, StringComparison.Ordinal))
            {
                throw new ArgumentException("Requested currency does not match merchant activity currency.", nameof(requestedCurrency));
            }
        }

        Money.Zero(normalized);
        return normalized;
    }

    private static (int Count, decimal Volume) WindowStats(IReadOnlyList<FraudTransaction> transactions, DateTimeOffset asOf, TimeSpan window)
    {
        var cutoff = asOf - window;
        var matches = transactions
            .Where(transaction => transaction.OccurredAt >= cutoff && transaction.OccurredAt <= asOf)
            .ToList();

        return (matches.Count, matches.Where(transaction => transaction.Status != TransactionStatus.Declined).Sum(transaction => transaction.Amount.Amount));
    }

    private static bool IsRoundAmount(decimal amount) => amount >= 100m && amount % 100m == 0m;

    private static decimal Ratio(int part, int total) => total == 0 ? 0m : Math.Round(part / (decimal)total, 6);

    private static decimal Ratio(decimal part, decimal total) => total == 0m ? 0m : Math.Round(part / total, 6);
}
