using MerchantFraud.Core.Domain;
using MerchantFraud.Core.Scoring;
using MerchantFraud.Persistence;

namespace MerchantFraud.Tests.Persistence;

public class FraudStoreTests
{
    [Fact]
    public void Store_RoundTripsCoreEntitiesAndAuditEvents()
    {
        using var store = new FraudStore("Data Source=:memory:");
        var merchant = new Merchant("MER-1", "Example LLC", "Example", "US", "https://example.com", MerchantLifecycle.Active);
        var transaction = new FraudTransaction(
            "T1",
            "MER-1",
            new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero),
            new Money(100.50m, "USD"),
            TransactionChannel.CardNotPresent,
            TransactionStatus.Captured,
            "visa",
            "411111",
            "1111",
            "CUST-1",
            "DEV-1",
            "203.0.0.0",
            "EXT-1");
        var dispute = new FraudDispute(
            "D1",
            "T1",
            "MER-1",
            new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero),
            new Money(100.50m, "USD"),
            "fraud",
            DisputeStatus.Open,
            externalRef: "EXT-D1");
        var score = new FraudScoreResult(
            "S1",
            FraudScoreSubjectType.Merchant,
            "MER-1",
            new FraudRiskScore(812, new[] { "DISPUTE_RATE_CRITICAL", "FEATURE_DISPUTE_RATE" }),
            new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero),
            "rules-v1");
        var fraudCase = new FraudCase(
            "C1",
            "MER-1",
            "Review merchant",
            CasePriority.High,
            CaseStatus.Open,
            "analyst-1",
            new DateTimeOffset(2026, 1, 3, 1, 0, 0, TimeSpan.Zero),
            externalRef: "EXT-C1");

        store.UpsertMerchant(merchant);
        store.UpsertTransaction(transaction);
        store.UpsertDispute(dispute);
        store.SaveScoreResult(score);
        store.UpsertCase(fraudCase);
        store.AddAuditEvent("system", "score", "score_result", "S1", "created", score.ScoredAt, "A1");

        Assert.Equal("Example LLC", Assert.Single(store.GetMerchants()).LegalName);

        var storedTransaction = Assert.Single(store.GetTransactions("MER-1"));
        Assert.Equal(100.50m, storedTransaction.Amount.Amount);
        Assert.Equal("USD", storedTransaction.Amount.Currency);
        Assert.Equal(TransactionChannel.CardNotPresent, storedTransaction.Channel);
        Assert.Equal("1111", storedTransaction.CardLast4);
        Assert.Equal("203.0.0.0", storedTransaction.IpAddress);

        var storedDispute = Assert.Single(store.GetDisputes("MER-1"));
        Assert.Equal("fraud", storedDispute.Reason);
        Assert.Equal(DisputeStatus.Open, storedDispute.Status);
        Assert.Equal("EXT-D1", storedDispute.ExternalRef);

        var storedScore = Assert.Single(store.GetScoreResults("MER-1"));
        Assert.Equal(812, storedScore.Score.Value);
        Assert.Equal(FraudRiskBand.Critical, storedScore.Score.Band);
        Assert.Equal(new[] { "DISPUTE_RATE_CRITICAL", "FEATURE_DISPUTE_RATE" }, storedScore.Score.ReasonCodes);
        Assert.Equal("rules-v1", storedScore.ModelVersion);

        var storedCase = Assert.Single(store.GetCases("MER-1"));
        Assert.Equal(CasePriority.High, storedCase.Priority);
        Assert.Equal("analyst-1", storedCase.AssignedTo);
        Assert.Equal("EXT-C1", storedCase.ExternalRef);

        var auditEvent = Assert.Single(store.GetAuditEvents("S1"));
        Assert.Equal("system", auditEvent.Actor);
        Assert.Equal("score", auditEvent.Action);
        Assert.Equal("score_result", auditEvent.EntityType);
        Assert.Equal("created", auditEvent.Details);
    }

    [Fact]
    public void Store_UpsertTransaction_ReplacesExistingRow()
    {
        using var store = new FraudStore("Data Source=:memory:");
        var transaction = Transaction("T1", 100m, TransactionStatus.Captured);

        store.UpsertTransaction(transaction);
        store.UpsertTransaction(transaction with { Amount = new Money(200m, "USD"), Status = TransactionStatus.Settled });

        var stored = Assert.Single(store.GetTransactions("MER-1"));
        Assert.Equal(200m, stored.Amount.Amount);
        Assert.Equal(TransactionStatus.Settled, stored.Status);
    }

    [Fact]
    public void Store_EmptyDatabase_ReturnsEmptyLists()
    {
        using var store = new FraudStore("Data Source=:memory:");

        Assert.Empty(store.GetMerchants());
        Assert.Empty(store.GetTransactions("MER-1"));
        Assert.Empty(store.GetDisputes("MER-1"));
        Assert.Empty(store.GetScoreResults("MER-1"));
        Assert.Empty(store.GetCases("MER-1"));
        Assert.Empty(store.GetAuditEvents());
    }

    private static FraudTransaction Transaction(string id, decimal amount, TransactionStatus status) =>
        new(
            id,
            "MER-1",
            new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero),
            new Money(amount, "USD"),
            TransactionChannel.CardPresent,
            status);
}
