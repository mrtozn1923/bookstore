# Bookstore API inventory

One row per API surface. The contract column is the source of truth; the drift check (npm run drift)
keeps code and contract in line.

| API | Audience | Owner | Contract | Status | Notes |
|---|---|---|---|---|---|
| Catalog (/books) | public, bookstore-web | Catalog team | contract/bookstore.openapi.yaml | api-version 2026-11-15; 2026-09-01 deprecated, sunset 2027-05-17 | |
| Ordering (/orders, /customers/{customerId}/cart) | bookstore-web, mobile | Ordering team | contract/bookstore.openapi.yaml | active | Idempotency-Key on POST /orders |
| Institutional partner API (/institutions, /purchase-approvals) | schools, libraries, companies | Institutional team | contract/bookstore.openapi.yaml | active | quota "batches" 5/min; webhooks planned |
| Operations (/operations) | all | Platform | contract/bookstore.openapi.yaml | active | status monitors kept 24 h |
| Problem types (/problems) | all | Platform | contract/bookstore.openapi.yaml | active | catalog of error types |
| Legacy RPC (/legacy/rpc) | old partner integrations | Catalog team | contract/bookstore.openapi.yaml (deprecated) | deprecated 2026-11-15, sunset 2027-03-31 | usage logged per client |
