# Workbench

`web/workbench` is the Angular fraud-operations UI.

## Run locally

Start the API on port 5292:

```bash
dotnet run --project src/MerchantFraud.Api --urls http://localhost:5292
```

Start the workbench:

```bash
cd web/workbench
npm install
npm start
```

The dev server proxies `/api` calls to the API on `http://localhost:5292`.

## Pages

- Dashboard — API status, high-risk alert count, open case count, recent alerts.
- Alerts — full alerts queue with score band and reason codes.
- Merchant detail — `/merchants/:id` shows score explanations and cases for one merchant.
- Preset Lab — `/presets` runs a standalone preset and reveals each component stage in order (synthetic data, MapWright mapping, ingestion and masking, features, rules, network, MerchantIntelligence, ML, scoring, alerts/cases). Each stage card shows status, summary and metrics; click a card to expand its tables. A final table shows the outcome per merchant. Untick **Save results to Alerts & Cases** for a dry run.

## Branding

The workbench is branded **FraudShield**. The logo is `src/app/shared/brand-logo.ts` (`<mf-brand-logo>`); the same mark is used for `public/favicon.svg`, `favicon.ico` and `apple-touch-icon.png`.
