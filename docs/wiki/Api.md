# API

`MerchantFraud.Api` is an ASP.NET Core minimal API over the fraud pipeline and SQLite store.

## Endpoints

- `GET /health` — service health.
- `POST /ingest/transactions` — accepts a JSON transaction feed, parses it with `TransactionFeedParser`, stores valid transactions, writes an audit event, and returns `{ stored, errors }`.
- `POST /score/merchant` — loads stored transactions/disputes for a merchant, extracts features, evaluates rules, computes a 0–1000 score, persists the `FraudScoreResult`, writes an audit event, and returns score value/band/reason codes/action/signals.
- `GET /alerts` — returns up to 100 high/critical score results ordered by score.
- `GET /cases?merchantId=` — lists cases, optionally filtered by merchant.
- `POST /cases` — creates a case and writes an audit event.

## Configuration

SQLite connection string:

```json
{
  "ConnectionStrings": {
    "FraudDb": "Data Source=merchantfraud.db"
  }
}
```

The default is `Data Source=merchantfraud.db`. Tests use `Data Source=:memory:`.

## Notes

JSON enum values are accepted as strings. Transaction ingest still uses the ingestion parser's field normalization and PII/PCI masking rules.
