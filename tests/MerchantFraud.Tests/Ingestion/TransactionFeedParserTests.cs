using MerchantFraud.Core.Domain;
using MerchantFraud.Ingestion;

namespace MerchantFraud.Tests.Ingestion;

public class SensitiveDataMaskerTests
{
    [Fact]
    public void MaskIpAddress_KeepsIpv4PrefixOnly()
    {
        Assert.Equal("203.0.0.0", SensitiveDataMasker.MaskIpAddress("203.0.113.10"));
    }

    [Theory]
    [InlineData("2001:db8::1")]
    [InlineData("not-an-ip")]
    public void MaskIpAddress_MasksNonIpv4Values(string value)
    {
        Assert.Equal("***", SensitiveDataMasker.MaskIpAddress(value));
    }

    [Fact]
    public void MaskCardNumber_ReturnsLast4Only()
    {
        Assert.Equal("1111", SensitiveDataMasker.MaskCardNumber("4111 1111 1111 1111"));
    }

    [Fact]
    public void MaskCardNumber_ReturnsNullForShortValues()
    {
        Assert.Null(SensitiveDataMasker.MaskCardNumber("1234"));
    }
}

public class TransactionFeedParserJsonTests
{
    [Fact]
    public void ParseJson_MapsTransactionsAndMasksIpByDefault()
    {
        const string json = """
        {
          "transactions": [
            {
              "id": "TXN-1",
              "merchantId": "MER-1",
              "occurredAt": "2026-01-01T12:00:00Z",
              "amount": 42.50,
              "currency": "usd",
              "channel": "card-not-present",
              "status": "captured",
              "cardBrand": "visa",
              "cardNumber": "4111111111111111",
              "ipAddress": "203.0.113.10",
              "deviceId": "DEV-1"
            }
          ]
        }
        """;

        var result = TransactionFeedParser.ParseJson(json);

        var transaction = Assert.Single(result.Transactions);
        Assert.Empty(result.Errors);
        Assert.Equal("TXN-1", transaction.Id);
        Assert.Equal("MER-1", transaction.MerchantId);
        Assert.Equal(new MerchantFraud.Core.Domain.Money(42.50m, "USD"), transaction.Amount);
        Assert.Equal(TransactionChannel.CardNotPresent, transaction.Channel);
        Assert.Equal(TransactionStatus.Captured, transaction.Status);
        Assert.Equal("1111", transaction.CardLast4);
        Assert.Equal("203.0.0.0", transaction.IpAddress);
        Assert.Equal("DEV-1", transaction.DeviceId);
    }

    [Fact]
    public void ParseJson_CollectsRowErrorsWithoutFailingValidRows()
    {
        const string json = """
        [
          { "id": "TXN-1", "merchantId": "MER-1", "occurredAt": "2026-01-01T12:00:00Z", "amount": 10, "currency": "USD" },
          { "id": "TXN-2", "merchantId": "MER-1", "occurredAt": "2026-01-01T12:01:00Z" }
        ]
        """;

        var result = TransactionFeedParser.ParseJson(json);

        Assert.Single(result.Transactions);
        Assert.Single(result.Errors);
        Assert.Contains("Row 1", result.Errors[0]);
        Assert.Contains("amount", result.Errors[0]);
    }

    [Fact]
    public void ParseJson_CanKeepUnmaskedIpWhenRequested()
    {
        const string json = """
        [{ "id": "TXN-1", "merchantId": "MER-1", "occurredAt": "2026-01-01T12:00:00Z", "amount": 10, "currency": "USD", "ipAddress": "203.0.113.10" }]
        """;

        var result = TransactionFeedParser.ParseJson(json, new TransactionIngestionOptions(MaskSensitiveData: false));

        Assert.Equal("203.0.113.10", Assert.Single(result.Transactions).IpAddress);
    }
}

public class TransactionFeedParserCsvTests
{
    [Fact]
    public void ParseCsv_ParsesQuotedFieldsAndSnakeCaseHeaders()
    {
        const string csv = """
        transaction_id,merchant_id,occurred_at,amount,currency,channel,status,card_brand,card_bin,card_last4,customer_id,device_id,ip_address,external_ref
        "TXN-1","MER-1","2026-01-01T12:00:00Z","42.50","USD","CNP","Captured","visa","411111","1111","CUST-1","DEV,1","203.0.113.10","EXT-1"
        """;

        var result = TransactionFeedParser.ParseCsv(csv);

        var transaction = Assert.Single(result.Transactions);
        Assert.Empty(result.Errors);
        Assert.Equal("TXN-1", transaction.Id);
        Assert.Equal("MER-1", transaction.MerchantId);
        Assert.Equal(TransactionChannel.CardNotPresent, transaction.Channel);
        Assert.Equal("DEV,1", transaction.DeviceId);
        Assert.Equal("203.0.0.0", transaction.IpAddress);
        Assert.Equal("EXT-1", transaction.ExternalRef);
    }

    [Fact]
    public void ParseCsv_ReturnsErrorForEmptyFeed()
    {
        var result = TransactionFeedParser.ParseCsv("");

        Assert.Empty(result.Transactions);
        Assert.Single(result.Errors);
        Assert.Contains("empty", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }
}
