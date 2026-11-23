# Ingestion

`MerchantFraud.Ingestion` converts partner transaction feeds into canonical `FraudTransaction` records.

## Supported inputs

- **JSON** — either a top-level array or an object with a `transactions` array.
- **CSV** — first row is the header; quoted fields and CRLF are supported.

Field names are normalized, so camelCase, snake_case, and common aliases are accepted (`transactionId`, `transaction_id`, `occurredAt`, `occurred_at`, `timestamp`, `createdAt`).

## Required fields

`id`, `merchantId`, `occurredAt`, `amount`, and `currency` are required. Optional fields include channel, status, card brand/BIN/last4, customer id, device id, IP address, and external reference.

## Errors

Parsing is row-tolerant: malformed rows are returned in `TransactionFeedResult.Errors` with a row number while valid rows still produce transactions.

## PII / PCI masking

By default (`TransactionIngestionOptions.MaskSensitiveData = true`):

- IPv4 addresses are masked to the first two octets (`203.0.113.10` → `203.0.0.0`); non-IPv4 values become `***`.
- Raw card numbers are never stored. If only `cardNumber`/`pan` is supplied, only the last 4 digits are kept as `cardLast4`.
- `cardBin` and `cardLast4` are retained because BIN + last4 is the PCI-safe identifier pair.

Set `MaskSensitiveData: false` only for trusted local development feeds.
