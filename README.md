# MerchantFraud

Hybrid fraud-risk platform for merchant acquiring: explainable rules + ML scoring for application fraud, transaction abuse, chargebacks, and merchant network rings. .NET 10, Angular, SQLite.

## Goals

- Detect merchant application fraud, synthetic identities, bust-out behavior, transaction abuse, chargeback spikes, refund abuse, and linked merchant rings.
- Produce explainable 0–1000 fraud risk scores with reason codes.
- Keep deterministic rules first; ML models are optional scoring inputs, not black-box decision makers.
- Never store raw PAN or CVV. Mask PII and sensitive financial data by default.

## Current status

Early scaffold. The solution currently contains:

- `src/MerchantFraud.Core` — canonical fraud domain model and fraud risk score primitive.
- `src/MerchantFraud.Ingestion` — JSON/CSV transaction feed parsing with row-level errors and PII/PCI masking.
- `tests/MerchantFraud.Tests` — xUnit tests.
- `docs/wiki` — architecture, domain model, ingestion, roadmap, and planning docs.

Planned projects include feature extraction, rules, model scoring, case management, persistence, API, and an Angular workbench.

## Build and test

```bash
dotnet build MerchantFraud.slnx
dotnet test MerchantFraud.slnx
```

## Documentation

- [Wiki home](docs/wiki/Home.md)
- [Architecture](docs/wiki/Architecture.md)
- [Roadmap](docs/wiki/Roadmap.md)

## Principles

1. Missing evidence is never treated as clear.
2. Every score carries reason codes and evidence.
3. Rules and workflows are versioned data where practical.
4. Analyst decisions and dispute outcomes feed back as labels.
5. Free/public/synthetic data first; paid providers are optional integrations.
