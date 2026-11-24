using System.Globalization;
using MerchantFraud.Core.Domain;
using MerchantFraud.Integrations;

namespace MerchantFraud.Presets;

public sealed record DemoMerchant(
    Merchant Merchant,
    string Scenario,
    bool ExpectedFraud,
    MerchantIntelligenceAssessment Assessment);

public sealed record DemoPartnerFeed(
    MapWrightTransactionProfile Profile,
    IReadOnlyList<IReadOnlyDictionary<string, string?>> Rows,
    IReadOnlyList<FraudDispute> Disputes);

public static class DemoMerchantScenarios
{
    public const string Bakery = "MER-DEMO-BAKERY";
    public const string GiftCards = "MER-DEMO-GIFTCARD";
    public const string Gadgets = "MER-DEMO-GADGETS";
    public const string Ring1 = "MER-DEMO-RING-1";
    public const string Ring2 = "MER-DEMO-RING-2";
    public const string Ring3 = "MER-DEMO-RING-3";
    public const string Ring4 = "MER-DEMO-RING-4";

    public static readonly DateTimeOffset AsOf = new(2026, 1, 31, 12, 0, 0, TimeSpan.Zero);

    public static IReadOnlyList<string> RingMerchantIds { get; } = new[] { Ring1, Ring2, Ring3, Ring4 };

    public static MapWrightTransactionProfile PartnerProfile { get; } = new(
        "acme-gateway-v2",
        new Dictionary<string, string>
        {
            ["id"] = "txn_ref",
            ["merchantId"] = "mid",
            ["occurredAt"] = "event_time",
            ["amount"] = "amt",
            ["currency"] = "ccy",
            ["channel"] = "entry_mode",
            ["status"] = "result",
            ["cardBrand"] = "scheme",
            ["cardNumber"] = "pan",
            ["cardBin"] = "bin",
            ["cardLast4"] = "last4",
            ["customerId"] = "shopper_id",
            ["deviceId"] = "device_fp",
            ["ipAddress"] = "client_ip",
            ["externalRef"] = "ref"
        });

    public static IReadOnlyList<DemoMerchant> All { get; } = new[]
    {
        Demo(Bakery, "Maple Street Bakery LLC", "Maple Street Bakery", "Clean neighborhood merchant (control)", false,
            MerchantIntelligenceDecision.Approve, 140),
        Demo(GiftCards, "QuickGift Cards Online Inc", "QuickGift", "Card-testing burst on a gift-card storefront", true,
            MerchantIntelligenceDecision.Refer, 620, "HIGH_RISK_MCC", "WEBSITE_POLICY_GAPS"),
        Demo(Gadgets, "TrendyGadgets Outlet LLC", "TrendyGadgets", "Chargeback and refund abuse", true,
            MerchantIntelligenceDecision.Approve, 410, "REFUND_POLICY_MISSING"),
        Demo(Ring1, "Northwind Supply Co", "Northwind Supply", "Merchant ring — shared device, shopper and card", true,
            MerchantIntelligenceDecision.Decline, 790, "SHARED_OWNER_ADDRESS", "THIN_FILE", "SANCTIONS_NEAR_MATCH"),
        Demo(Ring2, "Bluepeak Trading LLC", "Bluepeak Trading", "Merchant ring — shared device, shopper and card", true,
            MerchantIntelligenceDecision.Refer, 560, "SHARED_OWNER_ADDRESS", "THIN_FILE"),
        Demo(Ring3, "Silverline Goods Inc", "Silverline Goods", "Merchant ring — shared device, shopper and card", true,
            MerchantIntelligenceDecision.Refer, 520, "SHARED_OWNER_ADDRESS"),
        Demo(Ring4, "Crestview Wholesale LLC", "Crestview Wholesale", "Merchant ring — shared device, shopper and card", true,
            MerchantIntelligenceDecision.Approve, 300, "THIN_FILE")
    };

    public static DemoPartnerFeed BuildFeed(IReadOnlyCollection<string> merchantIds)
    {
        var rows = new List<IReadOnlyDictionary<string, string?>>();
        var disputes = new List<FraudDispute>();
        var selected = new HashSet<string>(merchantIds, StringComparer.Ordinal);

        if (selected.Contains(Bakery))
        {
            AddBakery(rows);
        }

        if (selected.Contains(GiftCards))
        {
            AddGiftCards(rows);
        }

        if (selected.Contains(Gadgets))
        {
            AddGadgets(rows, disputes);
        }

        var ringIndex = 0;
        foreach (var ringMerchant in RingMerchantIds)
        {
            if (selected.Contains(ringMerchant))
            {
                AddRingMerchant(rows, disputes, ringMerchant, ringIndex);
            }

            ringIndex++;
        }

        if (rows.Count > 0)
        {
            var broken = new Dictionary<string, string?>(rows[0]) { ["txn_ref"] = "TX-BROKEN-0001", ["amt"] = null };
            rows.Add(broken);
        }

        return new DemoPartnerFeed(PartnerProfile, rows, disputes);
    }

    private static DemoMerchant Demo(
        string id,
        string legalName,
        string tradingName,
        string scenario,
        bool expectedFraud,
        MerchantIntelligenceDecision decision,
        int riskScore,
        params string[] reasonCodes) =>
        new(
            new Merchant(id, legalName, tradingName, "US", lifecycle: MerchantLifecycle.Active),
            scenario,
            expectedFraud,
            new MerchantIntelligenceAssessment(id, $"MI-{id}", decision, riskScore, reasonCodes, AsOf.AddDays(-45)));

    private static void AddBakery(List<IReadOnlyDictionary<string, string?>> rows)
    {
        var random = new Random(101);
        for (var index = 0; index < 40; index++)
        {
            var amount = Math.Round((decimal)(random.NextDouble() * 38 + 6.25), 2);
            if (amount % 1m == 0m)
            {
                amount += 0.35m;
            }

            rows.Add(Row(
                $"TX-BAKERY-{index:D4}",
                Bakery,
                AsOf.AddDays(-random.Next(0, 30)).AddHours(-random.Next(1, 10)),
                amount,
                index % 4 == 0 ? "ecommerce" : "cp",
                index == 17 ? "refunded" : "captured",
                index % 2 == 0 ? "visa" : "mastercard",
                pan: null,
                bin: (index % 2 == 0 ? 414720 : 520082).ToString(CultureInfo.InvariantCulture),
                last4: (1000 + index * 37).ToString(CultureInfo.InvariantCulture),
                shopper: $"CUS-BAKERY-{index % 28:D3}",
                device: $"POS-BAKERY-{index % 2 + 1}",
                ip: $"73.12.{index % 8}.{20 + index}"));
        }
    }

    private static void AddGiftCards(List<IReadOnlyDictionary<string, string?>> rows)
    {
        var random = new Random(202);
        var roundTickets = new[] { 25m, 50m, 100m, 200m };
        for (var index = 0; index < 140; index++)
        {
            var inBurst = index >= 20;
            var occurredAt = inBurst
                ? AsOf.AddMinutes(-random.Next(5, 23 * 60))
                : AsOf.AddDays(-random.Next(2, 30)).AddHours(-random.Next(0, 12));
            var declined = inBurst && random.NextDouble() < 0.70;
            var amount = index == 7 ? 2500m : roundTickets[random.Next(roundTickets.Length)];
            var cardSuffix = (1000 + random.Next(0, 9000)).ToString(CultureInfo.InvariantCulture);
            rows.Add(Row(
                $"TX-GIFT-{index:D4}",
                GiftCards,
                occurredAt,
                amount,
                "online",
                declined ? "declined" : "captured",
                "visa",
                pan: $"400000{random.Next(100000, 999999)}{cardSuffix}",
                bin: "400000",
                last4: null,
                shopper: $"CUS-GIFT-{random.Next(1, 400):D3}",
                device: $"DEV-GIFT-{random.Next(1, 90):D2}",
                ip: $"185.220.{random.Next(0, 255)}.{random.Next(1, 254)}"));
        }
    }

    private static void AddGadgets(List<IReadOnlyDictionary<string, string?>> rows, List<FraudDispute> disputes)
    {
        var random = new Random(303);
        var reasons = new[] { "fraud", "not_received", "not_as_described" };
        for (var index = 0; index < 60; index++)
        {
            var id = $"TX-GADGET-{index:D4}";
            var occurredAt = AsOf.AddDays(-random.Next(1, 30)).AddHours(-random.Next(0, 20));
            var amount = Math.Round((decimal)(random.NextDouble() * 420 + 79.99), 2);
            var status = index % 5 == 0 ? "refunded" : index % 13 == 0 ? "chargeback" : "captured";
            rows.Add(Row(
                id,
                Gadgets,
                occurredAt,
                amount,
                "ecommerce",
                status,
                index % 3 == 0 ? "amex" : "mastercard",
                pan: null,
                bin: (index % 3 == 0 ? 371449 : 545454).ToString(CultureInfo.InvariantCulture),
                last4: (2000 + index * 53).ToString(CultureInfo.InvariantCulture),
                shopper: $"CUS-GADGET-{index:D3}",
                device: $"DEV-GADGET-{index % 45:D2}",
                ip: $"98.44.{index % 30}.{10 + index}"));

            if (index % 7 == 3)
            {
                var opened = occurredAt.AddDays(random.Next(3, 9));
                disputes.Add(new FraudDispute(
                    $"DSP-GADGET-{disputes.Count + 1:D3}",
                    id,
                    Gadgets,
                    opened > AsOf ? AsOf : opened,
                    new Money(amount, "USD"),
                    reasons[disputes.Count % reasons.Length],
                    disputes.Count % 3 == 0 ? DisputeStatus.Lost : DisputeStatus.Open));
            }
        }
    }

    private static void AddRingMerchant(
        List<IReadOnlyDictionary<string, string?>> rows,
        List<FraudDispute> disputes,
        string merchantId,
        int ringIndex)
    {
        var random = new Random(404 + ringIndex);
        var prefix = $"TX-RING{ringIndex + 1}";
        for (var index = 0; index < 25; index++)
        {
            var shared = index % 3 == 0;
            var id = $"{prefix}-{index:D4}";
            var occurredAt = AsOf.AddDays(-random.Next(0, 20)).AddHours(-random.Next(0, 23));
            var amount = index % 2 == 0 ? random.Next(3, 9) * 100m : Math.Round((decimal)(random.NextDouble() * 300 + 50), 2);
            rows.Add(Row(
                id,
                merchantId,
                occurredAt,
                amount,
                "online",
                "captured",
                "visa",
                pan: null,
                bin: shared ? "424242" : (431940 + ringIndex).ToString(CultureInfo.InvariantCulture),
                last4: shared ? "4242" : (3000 + ringIndex * 100 + index).ToString(CultureInfo.InvariantCulture),
                shopper: shared ? "CUS-RING-001" : $"CUS-RING{ringIndex + 1}-{index:D3}",
                device: shared ? "DEV-RING-7731" : $"DEV-RING{ringIndex + 1}-{index % 6}",
                ip: shared ? $"45.83.{index}.{ringIndex + 10}" : $"{60 + ringIndex}.21.{index}.7"));

            if (index is 4 or 11)
            {
                disputes.Add(new FraudDispute(
                    $"DSP-RING{ringIndex + 1}-{index:D3}",
                    id,
                    merchantId,
                    occurredAt.AddDays(2) > AsOf ? AsOf : occurredAt.AddDays(2),
                    new Money(amount, "USD"),
                    "fraud"));
            }
        }
    }

    private static Dictionary<string, string?> Row(
        string id,
        string merchantId,
        DateTimeOffset occurredAt,
        decimal amount,
        string channel,
        string status,
        string scheme,
        string? pan,
        string bin,
        string? last4,
        string shopper,
        string device,
        string ip) =>
        new(StringComparer.Ordinal)
        {
            ["txn_ref"] = id,
            ["mid"] = merchantId,
            ["event_time"] = occurredAt.ToString("O", CultureInfo.InvariantCulture),
            ["amt"] = amount.ToString(CultureInfo.InvariantCulture),
            ["ccy"] = "USD",
            ["entry_mode"] = channel,
            ["result"] = status,
            ["scheme"] = scheme,
            ["pan"] = pan,
            ["bin"] = bin,
            ["last4"] = last4,
            ["shopper_id"] = shopper,
            ["device_fp"] = device,
            ["client_ip"] = ip,
            ["ref"] = $"acme-{id.ToLowerInvariant()}"
        };
}
