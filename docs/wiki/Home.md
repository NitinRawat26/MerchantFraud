# MerchantFraud wiki

MerchantFraud is a hybrid fraud-risk platform for merchant acquiring. It is designed to score merchant applications, transaction batches, chargeback behavior, and linked merchant networks with explainable rules and optional ML models.

## Pages

- [Architecture](Architecture.md) — planned system boundaries and design principles.
- [Domain model](Domain-Model.md) — canonical merchant, transaction, dispute, case, and score records.
- [Ingestion](Ingestion.md) — JSON/CSV feed parsing and PII/PCI masking.
- [Features](Features.md) — merchant-level feature extraction.
- [Rules](Rules.md) — deterministic fraud rules and recommended actions.
- [Scoring](Scoring.md) — explainable 0–1000 fraud risk scoring.
- [Persistence](Persistence.md) — SQLite storage for fraud records.
- [API](Api.md) — REST endpoints for ingest, scoring, alerts, and cases.
- [Roadmap](Roadmap.md) — patch-by-patch build plan.

## Current implementation

- `MerchantFraud.Core` contains the canonical fraud domain model and the fraud risk score primitive.
- `MerchantFraud.Ingestion` parses JSON/CSV transaction feeds into canonical transactions and masks IP/card data by default.
- `MerchantFraud.Features` computes merchant feature sets from transactions and disputes.
- `MerchantFraud.Rules` evaluates feature sets against a deterministic fraud rule catalog.
- `MerchantFraud.Scoring` combines rule hits and feature signals into capped fraud risk scores.
- `MerchantFraud.Persistence` stores canonical records and audit events in SQLite.
- `MerchantFraud.Api` exposes the fraud pipeline over HTTP.
- `MerchantFraud.Tests` verifies score bounds, bands, reason-code normalization, domain invariants, ingestion behavior, feature extraction, rule evaluation, scoring, persistence, and API endpoints.
