using System.Globalization;
using MerchantFraud.Core.Domain;

namespace MerchantFraud.Synthetic;

public static class SyntheticDataGenerator
{
    private static readonly DateTimeOffset DefaultAsOf = new(2026, 1, 31, 12, 0, 0, TimeSpan.Zero);

    public static SyntheticDataset Generate(int merchantCount = 12, int seed = 42, DateTimeOffset? asOf = null)
    {
        if (merchantCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(merchantCount), merchantCount, "Merchant count must be at least 1.");
        }

        var random = new Random(seed);
        var end = asOf ?? DefaultAsOf;
        var scenarios = new List<SyntheticMerchantScenario>(merchantCount);

        for (var index = 0; index < merchantCount; index++)
        {
            var profile = ProfileFor(index);
            scenarios.Add(CreateScenario(index, profile, random, end));
        }

        return new SyntheticDataset(scenarios);
    }

    private static SyntheticFraudType ProfileFor(int index) => (index % 4) switch
    {
        0 => SyntheticFraudType.TransactionFraud,
        1 => SyntheticFraudType.ChargebackAbuse,
        2 => SyntheticFraudType.ApplicationFraud,
        _ => SyntheticFraudType.None
    };

    private static SyntheticMerchantScenario CreateScenario(int index, SyntheticFraudType profile, Random random, DateTimeOffset end)
    {
        var merchantId = $"MER-{index:D3}";
        var merchant = new Merchant(
            merchantId,
            $"Synthetic Merchant {index:D3} LLC",
            country: "US",
            lifecycle: MerchantLifecycle.Active);

        var transactionCount = profile == SyntheticFraudType.None ? random.Next(20, 36) : random.Next(28, 46);
        var transactions = new List<FraudTransaction>(transactionCount);

        for (var transactionIndex = 0; transactionIndex < transactionCount; transactionIndex++)
        {
            var occurredAt = end.AddDays(-random.Next(0, 30)).AddHours(-random.Next(0, 24));
            transactions.Add(new FraudTransaction(
                $"TX-{merchantId}-{transactionIndex:D4}",
                merchantId,
                occurredAt,
                new Money(AmountFor(profile, random), "USD"),
                ChannelFor(profile, random),
                StatusFor(profile, random),
                cardBrand: random.NextDouble() < 0.5 ? "visa" : "mastercard",
                cardBin: random.Next(400000, 500000).ToString(CultureInfo.InvariantCulture),
                cardLast4: random.Next(1000, 10000).ToString(CultureInfo.InvariantCulture),
                customerId: $"CUS-{index:D3}-{random.Next(1, 500):D3}",
                deviceId: $"DEV-{index:D3}-{random.Next(1, 50):D2}",
                ipAddress: $"198.51.100.{random.Next(1, 254)}",
                externalRef: $"ext-{merchantId}-{transactionIndex:D4}"));
        }

        var disputes = CreateDisputes(merchantId, profile, transactions, random, end);
        return new SyntheticMerchantScenario(merchant, transactions, disputes, LabelFor(profile));
    }

    private static decimal AmountFor(SyntheticFraudType profile, Random random)
    {
        if (profile == SyntheticFraudType.None)
        {
            return Math.Round((decimal)(random.NextDouble() * 180 + 20), 2);
        }

        if (random.NextDouble() < 0.30)
        {
            return random.Next(1, 10) * 100;
        }

        return Math.Round((decimal)(random.NextDouble() * 500 + 40), 2);
    }

    private static TransactionChannel ChannelFor(SyntheticFraudType profile, Random random)
    {
        if (profile == SyntheticFraudType.None)
        {
            return random.NextDouble() < 0.65 ? TransactionChannel.CardPresent : TransactionChannel.Ecommerce;
        }

        var roll = random.NextDouble();
        return roll < 0.80 ? TransactionChannel.CardNotPresent : roll < 0.95 ? TransactionChannel.Ecommerce : TransactionChannel.Moto;
    }

    private static TransactionStatus StatusFor(SyntheticFraudType profile, Random random)
    {
        var roll = random.NextDouble();
        return profile switch
        {
            SyntheticFraudType.None => roll < 0.03 ? TransactionStatus.Declined : TransactionStatus.Captured,
            SyntheticFraudType.TransactionFraud => roll < 0.08 ? TransactionStatus.Refunded : roll < 0.14 ? TransactionStatus.Declined : TransactionStatus.Captured,
            SyntheticFraudType.ChargebackAbuse => roll < 0.04 ? TransactionStatus.Declined : roll < 0.07 ? TransactionStatus.ChargedBack : TransactionStatus.Captured,
            _ => roll < 0.05 ? TransactionStatus.Declined : TransactionStatus.Captured
        };
    }

    private static List<FraudDispute> CreateDisputes(
        string merchantId,
        SyntheticFraudType profile,
        IReadOnlyList<FraudTransaction> transactions,
        Random random,
        DateTimeOffset end)
    {
        var disputeRate = profile switch
        {
            SyntheticFraudType.None => 0.01,
            SyntheticFraudType.TransactionFraud => 0.06,
            SyntheticFraudType.ChargebackAbuse => 0.18,
            _ => 0.08
        };
        var reasons = new[] { "fraud", "not_received", "not_as_described", "credit_not_processed" };
        var disputes = new List<FraudDispute>();

        foreach (var transaction in transactions)
        {
            if (random.NextDouble() >= disputeRate)
            {
                continue;
            }

            var openedAt = transaction.OccurredAt.AddDays(random.Next(1, 10));
            if (openedAt > end)
            {
                openedAt = end;
            }

            var status = random.NextDouble() < 0.55
                ? DisputeStatus.Open
                : random.NextDouble() < 0.5
                    ? DisputeStatus.UnderReview
                    : DisputeStatus.Lost;
            disputes.Add(new FraudDispute(
                $"DSP-{merchantId}-{disputes.Count + 1:D3}",
                transaction.Id,
                merchantId,
                openedAt,
                transaction.Amount,
                reasons[random.Next(reasons.Length)],
                status,
                closedAt: status == DisputeStatus.Lost ? openedAt.AddDays(random.Next(3, 20)) : null,
                externalRef: $"disp-{merchantId}-{disputes.Count + 1:D3}"));
        }

        return disputes;
    }

    private static SyntheticFraudLabel LabelFor(SyntheticFraudType profile) => profile switch
    {
        SyntheticFraudType.None => new SyntheticFraudLabel(profile, "Legitimate synthetic merchant."),
        SyntheticFraudType.TransactionFraud => new SyntheticFraudLabel(profile, "High CNP, round-ticket, and refund activity."),
        SyntheticFraudType.ChargebackAbuse => new SyntheticFraudLabel(profile, "Elevated dispute and chargeback activity."),
        SyntheticFraudType.ApplicationFraud => new SyntheticFraudLabel(profile, "Synthetic application risk signals."),
        _ => new SyntheticFraudLabel(profile, "Merchant network ring signals.")
    };
}
