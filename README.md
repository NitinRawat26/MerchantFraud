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
- `src/MerchantFraud.Features` — merchant-level feature extraction for activity, amount, refund, dispute, channel, and time-window behavior.
- `src/MerchantFraud.Rules` — deterministic fraud rules with review/decline actions and reason codes.
- `src/MerchantFraud.Scoring` — explainable 0–1000 fraud risk scoring from rule hits and feature signals.
- `src/MerchantFraud.Persistence` — SQLite store for merchants, transactions, disputes, score results, cases, and audit events.
- `src/MerchantFraud.Api` — ASP.NET Core minimal API for health, ingest, scoring, alerts, and cases.
- `src/MerchantFraud.Synthetic` — deterministic synthetic merchants, transactions, disputes, and fraud labels.
- `src/MerchantFraud.Ml` — supervised dataset builder, feature mapper, train/test split, metrics, and logistic-regression baseline.
- `src/MerchantFraud.Network` — shared-identifier merchant link detection and network risk scoring.
- `tests/MerchantFraud.Tests` — xUnit tests.
- `docs/wiki` — architecture, domain model, ingestion, features, rules, scoring, persistence, API, synthetic data, ML, network, roadmap, and planning docs.

Planned projects include richer ML trainers, case management, and an Angular workbench.

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
