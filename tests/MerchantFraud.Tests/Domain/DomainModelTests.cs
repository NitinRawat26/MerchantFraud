using MerchantFraud.Core.Domain;
using MerchantFraud.Core.Scoring;

namespace MerchantFraud.Tests.Domain;

public class MoneyTests
{
    [Fact]
    public void Constructor_NormalizesCurrency()
    {
        var money = new Money(12.34m, "usd");

        Assert.Equal(12.34m, money.Amount);
        Assert.Equal("USD", money.Currency);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void Constructor_RejectsNegativeAmounts(double amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money((decimal)amount, "USD"));
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("12D")]
    public void Constructor_RejectsInvalidCurrency(string currency)
    {
        Assert.Throws<ArgumentException>(() => new Money(1m, currency));
    }
}

public class FraudApplicationTests
{
    [Fact]
    public void Constructor_AcceptsConsistentAmounts()
    {
        var application = new FraudApplication(
            "APP-1",
            "MER-1",
            5812,
            new Money(120_000m, "USD"),
            new Money(45m, "USD"),
            new Money(500m, "USD"),
            cardNotPresentShare: 0.75);

        Assert.Equal(5812, application.MerchantCategoryCode);
        Assert.Equal(0.75, application.CardNotPresentShare);
        Assert.Equal("USD", application.AnnualVolume.Currency);
    }

    [Fact]
    public void Constructor_RejectsInvalidMcc()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FraudApplication(
            "APP-1",
            "MER-1",
            0,
            new Money(1m, "USD"),
            new Money(1m, "USD"),
            new Money(1m, "USD")));
    }

    [Fact]
    public void Constructor_RejectsMismatchedCurrencies()
    {
        Assert.Throws<ArgumentException>(() => new FraudApplication(
            "APP-1",
            "MER-1",
            5812,
            new Money(120_000m, "USD"),
            new Money(45m, "EUR"),
            new Money(500m, "USD")));
    }
}

public class FraudOwnerTests
{
    [Fact]
    public void Constructor_RejectsOwnershipAbove100()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FraudOwner(
            "OWN-1",
            "Ada Owner",
            ownershipPercent: 101));
    }
}

public class FraudTransactionTests
{
    [Fact]
    public void Constructor_AcceptsMaskedCardIdentifiers()
    {
        var transaction = new FraudTransaction(
            "TXN-1",
            "MER-1",
            DateTimeOffset.UtcNow,
            new Money(42.50m, "USD"),
            TransactionChannel.CardNotPresent,
            TransactionStatus.Captured,
            cardBrand: "visa",
            cardBin: "411111",
            cardLast4: "1111",
            ipAddress: "203.0.113.10");

        Assert.Equal("TXN-1", transaction.Id);
        Assert.Equal(TransactionChannel.CardNotPresent, transaction.Channel);
        Assert.Equal("411111", transaction.CardBin);
        Assert.Equal("1111", transaction.CardLast4);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("123456789")]
    public void Constructor_RejectsInvalidCardBin(string cardBin)
    {
        Assert.Throws<ArgumentException>(() => new FraudTransaction(
            "TXN-1",
            "MER-1",
            DateTimeOffset.UtcNow,
            new Money(42.50m, "USD"),
            cardBin: cardBin));
    }

    [Theory]
    [InlineData("123")]
    [InlineData("12345")]
    [InlineData("12A4")]
    public void Constructor_RejectsInvalidCardLast4(string cardLast4)
    {
        Assert.Throws<ArgumentException>(() => new FraudTransaction(
            "TXN-1",
            "MER-1",
            DateTimeOffset.UtcNow,
            new Money(42.50m, "USD"),
            cardLast4: cardLast4));
    }
}

public class FraudScoreResultTests
{
    [Fact]
    public void Constructor_CarriesScoreAndBand()
    {
        var result = new FraudScoreResult(
            "SCORE-1",
            FraudScoreSubjectType.Merchant,
            "MER-1",
            new FraudRiskScore(820, new[] { "VELOCITY_SPIKE" }),
            DateTimeOffset.UtcNow,
            modelVersion: "rules-v1");

        Assert.Equal(FraudRiskBand.Critical, result.Score.Band);
        Assert.Equal("rules-v1", result.ModelVersion);
        Assert.Equal(FraudScoreSubjectType.Merchant, result.SubjectType);
    }
}
