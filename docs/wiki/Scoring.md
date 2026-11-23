# Scoring

`MerchantFraud.Scoring` combines rule hits and feature signals into a capped 0–1000 `FraudRiskScore`.

## Result types

- `ScoreSignal` — one explainable contribution with a reason code, description, and point value.
- `MerchantScoringResult` — the persisted-ready `FraudScoreResult`, rule evaluation, feature set, signals, and uncapped raw score.

## Scoring model v1

Rule hits contribute by severity: Low 25, Medium 60, High 120, Critical 250. Feature signals add smaller continuous-risk points for elevated dispute rate, refund rate, decline rate, dispute-to-volume ratio, CNP dominance, round amounts, ticket outliers, and 24-hour velocity. The raw score is capped at 1000 before constructing `FraudRiskScore`.

Reason codes come from both rule hits and feature signals, so the score remains explainable before ML models are added.

## Usage

```csharp
var features = MerchantFeatureExtractor.Extract(merchantId, transactions, disputes, asOf: asOf);
var result = MerchantScoringEngine.Score(features, scoredAt: DateTimeOffset.UtcNow);
```

`MerchantScoringEngine.Score` evaluates the default rule catalog when no `FraudRuleEvaluation` is supplied. Pass `modelVersion` to stamp the scoring model/policy version.
