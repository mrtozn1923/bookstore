# ADR-0005: Conditional writes, Idempotency-Key and long-running operations

Status: Accepted (2026-11-15)

## Context
Two staff members edited the same book and the second save silently overwrote the first.
Customers double-click "Place order", and mobile clients retry after timeouts without
knowing whether the first request created an order. Invoice PDFs take several seconds
to generate, longer than some institutional integrations wait.

## Decision
- If-Match is required for every change to an existing resource (428 without it).
- POST /orders requires Idempotency-Key as in draft-ietf-httpapi-idempotency-key-header-08
  (not an RFC yet; we follow -08 and will review when it is published). Keys are per
  caller and operation, kept 24 hours; the fingerprint is the bound request body.
- Invoice generation is a long-running operation with an Azure-style status monitor
  (Operation-Location, Retry-After, NotStarted/Running/Succeeded/Failed/Canceled).
  We deviate from Azure in one point: the operation is started with POST on the
  invoices collection; Azure prefers PUT for long-running creates.

## Consequences
- Clients must keep the ETag of what they edit; bookstore-web shows a conflict screen on 412.
- The idempotency store is in memory in the sample; production needs a shared store
  (Solution Patterns, Idempotency Key) so every instance sees the same keys.
- Status monitors are deleted 24 hours after they end; clients must not keep their URLs.
