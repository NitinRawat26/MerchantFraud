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
