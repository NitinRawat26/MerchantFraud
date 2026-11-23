# Synthetic data

`MerchantFraud.Synthetic` provides a deterministic generator for sample merchants, transactions, disputes, and fraud labels.

## Usage

```csharp
var dataset = SyntheticDataGenerator.Generate(merchantCount: 12, seed: 42);

foreach (var scenario in dataset.Merchants)
{
    // scenario.Merchant
    // scenario.Transactions
    // scenario.Disputes
    // scenario.Label
}
```

## Behavior

- Same `merchantCount`, `seed`, and `asOf` produce the same dataset.
- Merchants cycle through legitimate, transaction-fraud, chargeback-abuse, and application-fraud profiles.
- Transactions are USD-only, use BIN/last4 card fields, and avoid raw PAN/CVV.
- Fraudulent profiles have higher CNP share, round/high tickets, refunds, declines, and dispute rates than legitimate merchants.
- Labels are synthetic ground truth for later supervised-model work, not real fraud outcomes.

## Counts

`SyntheticDataset` exposes `MerchantCount`, `TransactionCount`, `DisputeCount`, and `FraudulentMerchantCount`.
