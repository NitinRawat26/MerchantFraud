# MerchantFraud wiki

MerchantFraud is a hybrid fraud-risk platform for merchant acquiring. It is designed to score merchant applications, transaction batches, chargeback behavior, and linked merchant networks with explainable rules and optional ML models.

## Pages

- [Architecture](Architecture.md) — planned system boundaries and design principles.
- [Domain model](Domain-Model.md) — canonical merchant, transaction, dispute, case, and score records.
- [Ingestion](Ingestion.md) — JSON/CSV feed parsing and PII/PCI masking.
- [Roadmap](Roadmap.md) — patch-by-patch build plan.

## Current implementation

- `MerchantFraud.Core` contains the canonical fraud domain model and the fraud risk score primitive.
- `MerchantFraud.Ingestion` parses JSON/CSV transaction feeds into canonical transactions and masks IP/card data by default.
- `MerchantFraud.Tests` verifies score bounds, bands, reason-code normalization, domain invariants, and ingestion behavior.
