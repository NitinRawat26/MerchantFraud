# Network analysis

`MerchantFraud.Network` finds shared identifiers across merchant transaction activity and scores likely merchant rings.

## Link types

- `SharedDevice` — same `deviceId`.
- `SharedIpAddress` — same `ipAddress`.
- `SharedCustomer` — same `customerId`.
- `SharedCardFingerprint` — same card brand + BIN + last4.

## Output

`MerchantNetworkAnalyzer.Analyze` returns:

- `MerchantNetworkLink` records with merchant pair, link type, shared value, and shared transaction count.
- `MerchantNetworkScore` records with linked merchant count, link count, 0–1000 risk score, reason codes, and links.

Reason codes include `NETWORK_SHARED_DEVICE`, `NETWORK_SHARED_IP`, `NETWORK_SHARED_CUSTOMER`, `NETWORK_SHARED_CARD`, and `NETWORK_RING_SIZE` for larger rings.

## Notes

This is identifier-link analysis, not a full graph database. It uses only tokenized/PII-safe transaction fields already present in the canonical model.
