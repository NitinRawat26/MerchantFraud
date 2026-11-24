# ML foundation

`MerchantFraud.Ml` builds supervised training data from synthetic merchant scenarios and evaluates a baseline fraud model.

## Pieces

- `FraudFeatureMapper` maps `MerchantFeatureSet` into a fixed numeric feature vector.
- `FraudDatasetBuilder` extracts merchant features from `SyntheticDataset` scenarios and attaches synthetic labels.
- `FraudDatasetSplitter` performs a time-ordered train/test split.
- `FraudModelMetrics` computes precision, recall, F1, and accuracy at a threshold.
- `LogisticRegressionFraudModel` is a deterministic pure-.NET logistic-regression baseline behind `IFraudModel`.

## Usage

```csharp
var dataset = SyntheticDataGenerator.Generate(merchantCount: 60, seed: 42);
var examples = FraudDatasetBuilder.Build(dataset);
var split = FraudDatasetSplitter.Split(examples, trainFraction: 0.7);
var model = LogisticRegressionFraudModel.Fit(split.Train);
var probability = model.PredictProbability(example.Features);
```

This is the foundation for later champion/challenger model ops and richer trainers. Labels are synthetic until real outcome feedback is connected.
