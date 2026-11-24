# Model ops

`MerchantFraud.Ml` includes model-operations primitives for retraining, champion/challenger promotion, and drift checks.

## Trainer

`FraudModelTrainer.Train` wraps the existing pipeline:

1. Split examples with `FraudDatasetSplitter`.
2. Fit `LogisticRegressionFraudModel`.
3. Evaluate held-out examples with `FraudModelMetrics`.
4. Return `TrainedFraudModel` with the model, metrics, and version label.

## Registry

`FraudModelRegistry` tracks `FraudModelVersion` records with `Champion`, `Challenger`, or `Retired` status.

- `Register` adds a version and rejects duplicates or a second champion.
- `Promote` makes a version champion and retires the previous champion.
- `Retire` marks non-champion versions retired; the champion must be replaced first.

## Drift

`FraudDriftAnalyzer.Analyze` compares baseline and current `FraudTrainingExample` feature means. A feature is drifted when the absolute mean shift exceeds `threshold` relative to the baseline scale, producing a `FraudDriftReport` for operators before promotion.
