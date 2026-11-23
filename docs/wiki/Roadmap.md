# Roadmap

The build is intentionally patch-sized. Each patch should compile and pass tests before the next one starts.

## Patch plan

1. **Scaffold** — solution, Core project, tests, `.gitignore`, README, wiki skeleton.
2. **Canonical domain model** — merchant, owner, application, transaction, dispute, case, score-result, and money records (implemented in patch 02).
3. **Ingestion v1** — CSV/JSON transaction feed parsing and PII/PCI masking (implemented in patch 03).
4. **Feature extraction v1** — velocity, amount, refund, dispute, CNP, and time-window merchant features (implemented in patch 04).
5. **Rules engine v1** — deterministic fraud policies with hard stops and review triggers (implemented in patch 05).
6. **Scoring engine v1** — combine rule hits and features into a 0–1000 score with reason codes (implemented in patch 06).
7. **Persistence v1** — SQLite store for merchants, transactions, scores, cases, and audit events (implemented in patch 07).
8. **API v1** — health, ingest, score, alerts, and case endpoints (implemented in patch 08).
9. **Synthetic data generator** — deterministic sample merchants, transactions, disputes, and labels (implemented in patch 09).
10. **Supervised model foundation** — dataset builder, LightGBM training pipeline, offline evaluation.
11. **Network analysis v1** — shared identifiers and merchant link/ring scoring.
12. **MerchantIntelligence integration** — consume assessment/KYB output as fraud features.
13. **MapWright integration** — map partner-specific transaction/application schemas into canonical records.
14. **Angular workbench v1** — dashboard, alerts queue, merchant detail, score explanation.
15. **Model ops v1** — champion/challenger, drift checks, retrain/promote workflow.
16. **Docs and samples** — full wiki, README examples, demo data, and operator guides.

## Near-term priority

Post-boarding transaction and chargeback risk comes first because it is the clearest MVP and complements MerchantIntelligence's pre-boarding assessment focus.
