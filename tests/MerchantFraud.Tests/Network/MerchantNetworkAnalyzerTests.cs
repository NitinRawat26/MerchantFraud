using MerchantFraud.Core.Domain;
using MerchantFraud.Network;

namespace MerchantFraud.Tests.Network;

public class MerchantNetworkAnalyzerTests
{
    [Fact]
    public void Analyze_FindsSharedIdentifiersAndScoresMerchants()
    {
        var merchants = new[] { Merchant("MER-A"), Merchant("MER-B") };
        var transactions = new[]
        {
            Transaction("TX-A1", "MER-A", deviceId: "DEV-1", ipAddress: "198.51.100.10", customerId: "CUS-1", cardBin: "411111", cardLast4: "1111"),
            Transaction("TX-B1", "MER-B", deviceId: "DEV-1", ipAddress: "198.51.100.10", customerId: "CUS-1", cardBin: "411111", cardLast4: "1111"),
            Transaction("TX-A2", "MER-A", deviceId: "DEV-2", ipAddress: "198.51.100.20", customerId: "CUS-2", cardBin: "555555", cardLast4: "2222")
        };

        var analysis = MerchantNetworkAnalyzer.Analyze(merchants, transactions);

        Assert.Equal(4, analysis.Links.Count);
        Assert.Contains(analysis.Links, link => link.LinkType == NetworkLinkType.SharedDevice && link.SharedValue == "DEV-1");
        Assert.Contains(analysis.Links, link => link.LinkType == NetworkLinkType.SharedIpAddress && link.SharedValue == "198.51.100.10");
        Assert.Contains(analysis.Links, link => link.LinkType == NetworkLinkType.SharedCustomer && link.SharedValue == "CUS-1");
        Assert.Contains(analysis.Links, link => link.LinkType == NetworkLinkType.SharedCardFingerprint);
        Assert.Equal(2, analysis.Scores.Count);
        Assert.All(analysis.Scores, score => Assert.True(score.RiskScore > 0));
        Assert.All(analysis.Scores, score => Assert.Equal(1, score.LinkedMerchantCount));
        Assert.Contains(analysis.Scores, score => score.ReasonCodes.Contains("NETWORK_SHARED_DEVICE"));
        Assert.Contains(analysis.Scores, score => score.ReasonCodes.Contains("NETWORK_SHARED_CARD"));
    }

    [Fact]
    public void Analyze_NoSharedIdentifiers_ReturnsNoLinksOrScores()
    {
        var merchants = new[] { Merchant("MER-A"), Merchant("MER-B") };
        var transactions = new[]
        {
            Transaction("TX-A1", "MER-A", deviceId: "DEV-1", ipAddress: "198.51.100.10", customerId: "CUS-1", cardBin: "411111", cardLast4: "1111"),
            Transaction("TX-B1", "MER-B", deviceId: "DEV-2", ipAddress: "198.51.100.20", customerId: "CUS-2", cardBin: "555555", cardLast4: "2222")
        };

        var analysis = MerchantNetworkAnalyzer.Analyze(merchants, transactions);

        Assert.Empty(analysis.Links);
        Assert.Empty(analysis.Scores);
    }

    private static Merchant Merchant(string id) => new(id, $"{id} LLC", country: "US");

    private static FraudTransaction Transaction(
        string id,
        string merchantId,
        string? deviceId = null,
        string? ipAddress = null,
        string? customerId = null,
        string? cardBin = null,
        string? cardLast4 = null) =>
        new(
            id,
            merchantId,
            new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero),
            new Money(25m, "USD"),
            TransactionChannel.CardNotPresent,
            TransactionStatus.Captured,
            cardBin: cardBin,
            cardLast4: cardLast4,
            customerId: customerId,
            deviceId: deviceId,
            ipAddress: ipAddress);
}
