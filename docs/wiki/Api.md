# API

`MerchantFraud.Api` is an ASP.NET Core minimal API over the fraud pipeline and SQLite store.

## Endpoints

- `GET /api/health` — service health.
- `POST /api/ingest/transactions` — accepts a JSON transaction feed, parses it with `TransactionFeedParser`, stores valid transactions, writes an audit event, and returns `{ stored, errors }`.
- `POST /api/score/merchant` — loads stored transactions/disputes for a merchant, extracts features, evaluates rules, computes a 0–1000 score, persists the `FraudScoreResult`, writes an audit event, and returns score value/band/reason codes/action/signals.
- `GET /api/alerts` — returns up to 100 high/critical score results ordered by score.
- `GET /api/cases?merchantId=` — lists cases, optionally filtered by merchant.
- `POST /api/cases` — creates a case and writes an audit event.
- `GET /api/presets` — lists standalone presets (`full-platform`, `card-testing`, `chargeback-abuse`, `merchant-ring`, `clean-merchant`).
- `POST /api/presets/{id}/run?persist=true|false` — runs a preset through every component and returns per-stage status, summary, metrics and tables plus a per-merchant outcome. `persist` defaults to `true`, which saves merchants, transactions, disputes, scores, cases (`CASE-PRESET-*`) and audit events so results appear in Alerts and Cases; `persist=false` is a dry run. Re-running is idempotent. Unknown ids return 404.

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

When `wwwroot/index.html` exists, the API also serves the Angular workbench and falls back to `index.html` for non-`/api` routes so client-side routes like `/alerts` keep working.
