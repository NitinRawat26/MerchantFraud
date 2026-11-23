# Features

`MerchantFraud.Features` converts canonical transactions and disputes into a `MerchantFeatureSet` for rule and model scoring.

## Extraction

```csharp
var features = MerchantFeatureExtractor.Extract(
    merchantId,
    transactions,
    disputes,
    currency: null,
    asOf: DateTimeOffset.UtcNow);
```

Transactions and disputes are filtered to the requested merchant. If `asOf` is omitted, the latest transaction/dispute timestamp is used. Passing `asOf` gives point-in-time features by excluding later activity.

## Currency handling

Feature extraction v1 requires one currency per merchant. Mixed transaction/dispute currencies throw `ArgumentException`. Amount fields are decimals in `MerchantFeatureSet.Currency`.

## Feature groups

- **Activity** — transaction count, first/last transaction timestamps, decline/refund/chargeback counts and rates.
- **Amount** — total volume, average ticket, max ticket, round-amount count/share, max-to-average ticket ratio. Volume metrics exclude declined transactions.
- **Refunds** — refund volume, refund rate, and refund-to-volume ratio.
- **Disputes** — dispute count, open dispute count, dispute volume, dispute rate, and dispute-to-volume ratio.
- **Channel mix** — card-present, card-not-present, e-commerce, MOTO, and unknown channel shares.
- **Windows** — transaction counts and eligible volume for the last 24 hours, 7 days, and 30 days.

Round amounts are whole multiples of 100 and at least 100.
