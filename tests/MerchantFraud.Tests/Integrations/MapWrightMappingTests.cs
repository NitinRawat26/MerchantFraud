using System.Text.Json;
using MerchantFraud.Core.Domain;
using MerchantFraud.Ingestion;
using MerchantFraud.Integrations;

namespace MerchantFraud.Tests.Integrations;

public class MapWrightMappingTests
{
    [Fact]
    public void MapWrightMapper_MapsPartnerTransactionToCanonicalFeed()
    {
        var profile = new MapWrightTransactionProfile(
            "acme-pos",
            new Dictionary<string, string>
            {
                ["id"] = "txn_id",
                ["merchantId"] = "merchant_id",
                ["occurredAt"] = "ts",
                ["amount"] = "amt",
                ["cardBin"] = "cc_bin",
                ["cardLast4"] = "cc_last4",
                ["customerId"] = "cardholder",
                ["deviceId"] = "device_id",
                ["ipAddress"] = "remote_ip",
                ["externalRef"] = "ref"
            },
            new Dictionary<string, string>
            {
                ["currency"] = "USD",
                ["channel"] = "online",
                ["status"] = "captured"
            });
        var partnerFields = new Dictionary<string, string?>
        {
            ["txn_id"] = "TX-PARTNER-1",
            ["merchant_id"] = "MER-9",
            ["ts"] = "2026-01-01T12:00:00Z",
            ["amt"] = "123.45",
            ["cc_bin"] = "411111",
            ["cc_last4"] = "1111",
            ["cardholder"] = "CUS-9",
            ["device_id"] = "DEV-9",
            ["remote_ip"] = "203.0.113.10",
            ["ref"] = "partner-ref-1"
        };

        var mapped = MapWrightTransactionMapper.Map(partnerFields, profile);
        var json = JsonSerializer.Serialize(new { transactions = new[] { mapped } });
        var result = TransactionFeedParser.ParseJson(json);

        Assert.Empty(result.Errors);
        var transaction = Assert.Single(result.Transactions);
        Assert.Equal("TX-PARTNER-1", transaction.Id);
        Assert.Equal("MER-9", transaction.MerchantId);
        Assert.Equal(123.45m, transaction.Amount.Amount);
        Assert.Equal("USD", transaction.Amount.Currency);
        Assert.Equal(TransactionChannel.CardNotPresent, transaction.Channel);
        Assert.Equal("203.0.0.0", transaction.IpAddress);
        Assert.Equal("1111", transaction.CardLast4);
        Assert.Equal("partner-ref-1", transaction.ExternalRef);
    }

    [Fact]
    public void MapWrightMapper_MissingRequiredField_ProducesFeedError()
    {
        var profile = new MapWrightTransactionProfile(
            "acme-pos",
            new Dictionary<string, string>
            {
                ["id"] = "txn_id",
                ["merchantId"] = "merchant_id",
                ["occurredAt"] = "ts",
                ["amount"] = "amt"
            },
            new Dictionary<string, string> { ["currency"] = "USD" });
        var partnerFields = new Dictionary<string, string?>
        {
            ["txn_id"] = "TX-PARTNER-2",
            ["ts"] = "2026-01-01T12:00:00Z",
            ["amt"] = "10.00"
        };

        var mapped = MapWrightTransactionMapper.Map(partnerFields, profile);
        var json = JsonSerializer.Serialize(new { transactions = new[] { mapped } });
        var result = TransactionFeedParser.ParseJson(json);

        Assert.Empty(result.Transactions);
        Assert.Single(result.Errors);
        Assert.Contains("merchantid", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }
}
