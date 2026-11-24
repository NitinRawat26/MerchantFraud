# Presets

`MerchantFraud.Presets` runs scripted fraud scenarios through the whole platform so FraudShield can be tested standalone. Every run is deterministic (fixed seeds, as-of `2026-01-31T12:00:00Z`) and reports the output of each component separately.

## Presets

| Id | Merchants | Shows |
| --- | --- | --- |
| `full-platform` | All 7 demo merchants | Every scenario at once |
| `card-testing` | QuickGift + bakery control | 24h burst, round amounts, high decline rate |
| `chargeback-abuse` | TrendyGadgets + bakery control | Refunds, chargebacks and lost disputes |
| `merchant-ring` | 4 ring merchants + bakery control | Shared device, shopper and card fingerprint |
| `clean-merchant` | Maple Street Bakery | Healthy baseline |

## Stages

1. **Synthetic training data** (`MerchantFraud.Synthetic`) — labelled baseline (seed 42) and refresh (seed 7) datasets.
2. **Partner schema mapping** (`MapWrightTransactionMapper`) — demo rows arrive in a partner gateway schema and are mapped to canonical fields.
3. **Ingestion & masking** (`TransactionFeedParser`) — parses the feed, rejects one deliberately broken row, truncates IPs and reduces full PANs to last 4.
4. **Features** (`MerchantFeatureExtractor`) — decline/refund/dispute rates, CNP share, round amounts, 24h velocity, ticket size.
5. **Rules** (`FraudRuleEngine`) — catalog rule hits and recommended action per merchant.
6. **Network** (`MerchantNetworkAnalyzer`) — shared-identifier links and network scores.
7. **MerchantIntelligence** (`MerchantIntelligenceSignalMapper`) — onboarding decisions mapped to score signals.
8. **ML & model ops** (`FraudModelTrainer`, `FraudModelRegistry`, `FraudDriftAnalyzer`) — champion/challenger training, promotion, fraud probabilities and drift between training data and preset traffic.
9. **Scoring** (`MerchantScoringEngine`) — combines rules, features, MerchantIntelligence, network and ML signals into 0–1000 scores.
10. **Alerts, cases & audit** (`FraudStore`) — saves results, opens `CASE-PRESET-*` cases for High/Critical merchants that don't already have one, and writes one audit event per stage per run. Reruns don't duplicate merchants, scores or cases. Skipped on dry runs.

## Use

- API: `GET /api/presets`, `POST /api/presets/{id}/run?persist=true|false` (see [API](Api.md)).
- UI: workbench **Preset Lab** page (see [Workbench](Workbench.md)).
