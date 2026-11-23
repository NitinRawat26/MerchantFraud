# Persistence

`MerchantFraud.Persistence` stores canonical fraud records in SQLite using `Microsoft.Data.Sqlite`.

## Store

`FraudStore` opens a SQLite connection, creates the schema on construction, and exposes upsert/save/get methods for:

- `Merchant`
- `FraudTransaction`
- `FraudDispute`
- `FraudScoreResult`
- `FraudCase`
- audit events via `AddAuditEvent` / `GetAuditEvents`

Amounts are stored as invariant-culture decimal text, timestamps as ISO-8601 round-trip strings, enums as strings, and score reason codes as JSON.

## Usage

```csharp
using var store = new FraudStore("Data Source=merchantfraud.db");
store.UpsertMerchant(merchant);
store.UpsertTransaction(transaction);
store.SaveScoreResult(scoreResult);
store.AddAuditEvent("system", "score", "score_result", scoreResult.Id, "created");
```

For tests, use `new FraudStore("Data Source=:memory:")`; the connection stays open for the store lifetime.
