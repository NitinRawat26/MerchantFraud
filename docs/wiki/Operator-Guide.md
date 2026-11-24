# Operator guide

## Run the API

```bash
dotnet run --project src/MerchantFraud.Api --urls http://localhost:5292
```

## Ingest sample transactions

```bash
curl -X POST http://localhost:5292/api/ingest/transactions \
  -H "Content-Type: application/json" \
  --data-binary @samples/transaction-feed.sample.json
```

CSV feeds use the same endpoint with `Content-Type: text/csv` and the CSV body.

## Score and review

```bash
curl -X POST http://localhost:5292/api/score/merchant \
  -H "Content-Type: application/json" \
  -d '{"merchantId":"MER-SAMPLE-001"}'

curl http://localhost:5292/api/alerts
curl "http://localhost:5292/api/cases?merchantId=MER-SAMPLE-001"
```

Create a case for analyst follow-up:

```bash
curl -X POST http://localhost:5292/api/cases \
  -H "Content-Type: application/json" \
  -d '{"merchantId":"MER-SAMPLE-001","title":"Review high-risk transaction pattern","priority":"High"}'
```

## Run the workbench

```bash
cd web/workbench
npm ci
npm start
```

Open `http://localhost:4200`. The dev server proxies `/api` calls to `http://localhost:5292`.

## Run a standalone preset

Presets need no feed, external service or key. With the API running:

```bash
curl -X POST http://localhost:5292/api/presets/full-platform/run
```

Or open **Preset Lab** in the workbench and click a preset. See [Presets](Presets.md).

## Model operations

Use `MerchantFraud.Ml` primitives for offline retraining:

1. Build examples with `FraudDatasetBuilder`.
2. Train/evaluate with `FraudModelTrainer.Train`.
3. Check drift with `FraudDriftAnalyzer.Analyze`.
4. Register and promote versions with `FraudModelRegistry`.

## Safety

- Do not submit raw PAN or CVV. Use BIN/last4 or tokens only.
- IPs and card numbers are masked during ingestion by default.
- Treat missing evidence as unknown, not clear.
