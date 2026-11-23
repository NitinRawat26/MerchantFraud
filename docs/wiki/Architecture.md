# Architecture

## Status

Initial scaffold. Today the solution contains only `MerchantFraud.Core` and xUnit tests.

## Planned boundaries

| Area | Responsibility |
|---|---|
| `MerchantFraud.Core` | Canonical fraud domain model, scoring primitives, rules, features, and deterministic evaluation. |
| `MerchantFraud.Ingestion` | CSV/JSON/XML feed readers, schema normalization, PII/PCI masking. MapWright can map external schemas into the canonical model. |
| `MerchantFraud.Features` | Transaction, merchant, owner, financial, behavioral, and network feature extraction. |
| `MerchantFraud.Rules` | Deterministic fraud policies, hard stops, review triggers, and reason codes. |
| `MerchantFraud.Models` | Optional supervised and anomaly models, model registry, training, prediction, drift. |
| `MerchantFraud.Network` | Shared-identifier detection and merchant link/ring analysis. |
| `MerchantFraud.Store` | SQLite persistence for merchants, transactions, scores, cases, audit events, model versions. |
| `MerchantFraud.Api` | ASP.NET Core API for ingestion, scoring, cases, alerts, reports, and model operations. |
| `web/workbench` | Angular UI for queues, merchant 360, alerts, rules, workflows, and model governance. |

## Design principles

1. **Rules first.** Deterministic policies provide hard stops and explainable review triggers before any ML score is considered.
2. **Scores are evidence-backed.** Every score carries a 0–1000 value, band, and reason codes.
3. **Missing evidence is not clear.** Unavailable checks become coverage gaps, not low-risk signals.
4. **PII/PCI minimization.** Raw card numbers and CVVs are never accepted or stored; sensitive fields are masked.
5. **Free-first integrations.** Public data, synthetic feeds, and existing MerchantIntelligence checks come before paid providers.
6. **Feedback loop.** Analyst outcomes and dispute results become labels for evaluation and retraining.

## Planned scoring flow

```text
source feeds -> ingestion/mapping -> canonical fraud records
  -> feature extraction -> rules + model scores
  -> ensemble fraud risk score -> decision/case -> audit + feedback
```
