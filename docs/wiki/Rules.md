# Rules

`MerchantFraud.Rules` evaluates `MerchantFeatureSet` output with deterministic fraud policies.

## Types

- `FraudRule` — immutable rule definition with id, name, description, severity, action, reason code, condition, and message.
- `FraudRuleHit` — one triggered rule with reason code, severity, action, and message.
- `FraudRuleEvaluation` — merchant id, hits, recommended action, and highest severity.

## Actions and severity

Actions are ordered `None < Monitor < Review < Decline`. Severity is ordered `Low < Medium < High < Critical`. The evaluation recommends the strongest action and reports the highest severity across hits.

## Default catalog

Patch 05 includes review/decline rules for:

- dispute rate and dispute-to-volume ratio
- refund rate
- high decline rate
- dominant card-not-present mix
- 24-hour transaction velocity
- round-amount concentration
- maximum-ticket outliers

Rules are evaluated against features only, so the same catalog can later score applications, transactions, or merchant portfolios by supplying the right feature set.

## Usage

```csharp
var evaluation = FraudRuleEngine.Evaluate(features);
```

Pass a custom `IEnumerable<FraudRule>` to replace the default catalog for tests, policy packs, or partner-specific rules.
