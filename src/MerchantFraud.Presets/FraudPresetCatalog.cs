using static MerchantFraud.Presets.DemoMerchantScenarios;

namespace MerchantFraud.Presets;

public static class FraudPresetCatalog
{
    public static IReadOnlyList<FraudPreset> All { get; } = new[]
    {
        new FraudPreset(
            "full-platform",
            "Full platform walkthrough",
            "Seven demo merchants (clean control, card testing, chargeback abuse and a four-merchant ring) flow through every component: synthetic training data, MapWright mapping, ingestion and masking, features, rules, network, MerchantIntelligence, ML, scoring, alerts and cases.",
            GiftCards,
            new[] { Bakery, GiftCards, Gadgets, Ring1, Ring2, Ring3, Ring4 }),
        new FraudPreset(
            "card-testing",
            "Card-testing burst",
            "A gift-card storefront receives a 24-hour burst of round-amount, card-not-present attempts with a high decline rate. The clean bakery is included as a control.",
            GiftCards,
            new[] { GiftCards, Bakery }),
        new FraudPreset(
            "chargeback-abuse",
            "Chargeback and refund abuse",
            "An electronics outlet with elevated refunds, lost disputes and chargebacks. The clean bakery is included as a control.",
            Gadgets,
            new[] { Gadgets, Bakery }),
        new FraudPreset(
            "merchant-ring",
            "Merchant ring",
            "Four newly boarded merchants share one device, one shopper and one card fingerprint, so network analysis links them into a ring.",
            Ring1,
            new[] { Ring1, Ring2, Ring3, Ring4, Bakery }),
        new FraudPreset(
            "clean-merchant",
            "Clean merchant baseline",
            "A single low-risk bakery, to see what a healthy merchant looks like in every component.",
            Bakery,
            new[] { Bakery })
    };

    public static FraudPreset? Find(string id) =>
        All.FirstOrDefault(preset => string.Equals(preset.Id, id, StringComparison.OrdinalIgnoreCase));
}
