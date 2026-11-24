# Integrations

`MerchantFraud.Integrations` adapts external merchant risk outputs into fraud score signals.

## MerchantIntelligence

`MerchantIntelligenceAssessment` represents KYB/onboarding assessment output:

- `MerchantId`, `AssessmentId`, `AssessedAt`, `Source`
- `Decision` — `Approve`, `Refer`, or `Decline`
- `RiskScore` — canonical 0–1000 risk score
- `ReasonCodes`

`MerchantIntelligenceSignalMapper.Map` converts an assessment into `ScoreSignal` values:

- Decline decision: 250 points
- Refer decision: 120 points
- Risk score bands: high >=750, medium >=500, low >=250
- Aggregate reason-code signal capped at 30 points

`MerchantScoringEngine.Score` accepts optional `additionalSignals`, so MerchantIntelligence/KYB signals can be combined with rule hits and transaction features while preserving reason codes and the 0–1000 cap.

## MapWright

`MapWrightTransactionProfile` defines a partner-specific transaction schema:

- `Name` — partner/profile name.
- `FieldMappings` — canonical feed field to partner field, e.g. `merchantId -> merchant_id`.
- `Constants` — canonical feed fields with fixed values, e.g. `currency -> USD`.

`MapWrightTransactionMapper.Map` converts one partner record into canonical ingestion-feed fields. Serialize the result as a JSON `transactions` array and pass it to `TransactionFeedParser.ParseJson` to get canonical `FraudTransaction` records with normal masking and row-level errors.
