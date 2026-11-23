# Domain model

`MerchantFraud.Core.Domain` is the canonical fraud model. Ingestion, features, rules, and models should speak these types instead of carrying processor-specific payloads.

## Records

| Record | Purpose | Key invariants |
|---|---|---|
| `Merchant` | A boarded or prospective merchant | Required id and legal name; lifecycle state. |
| `FraudOwner` | Beneficial owner / principal | Required id and full name; ownership percent is 0–100 when present. |
| `FraudApplication` | Merchant application / underwriting request | MCC 1–9999; volume and ticket amounts share one currency; CNP share is 0–1. |
| `FraudTransaction` | One payment transaction | Required merchant and timestamp; amount is non-negative `Money`; optional BIN/last4 only. |
| `FraudDispute` | Chargeback / dispute event | Required transaction, merchant, amount, and reason. |
| `FraudCase` | Analyst review case | Required merchant and title; priority and status enums. |
| `FraudScoreResult` | A score attached to a subject | Wraps `FraudRiskScore` with subject type/id and optional model version. |
| `Money` | Currency-aware non-negative amount | Currency is normalized to a 3-letter ISO-style code. |

## PCI / PII guardrails

- `FraudTransaction` accepts `CardBin` and `CardLast4` only; it has no PAN or CVV fields.
- `CardBin` must be 6–8 digits and `CardLast4` exactly 4 digits when present.
- Ingestion must mask sensitive owner and account fields before persistence or model training.

## Enums

`MerchantLifecycle`, `TransactionChannel`, `TransactionStatus`, `DisputeStatus`, `CasePriority`, `CaseStatus`, and `FraudScoreSubjectType` keep downstream rules and UI filters stable.
