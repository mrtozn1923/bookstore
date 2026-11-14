# ADR-0002: Operations that need a verb

Status: Accepted (2026-11-15)

## Context
Cancelling an order is a state change with rules (only Pending or Confirmed orders),
not a field update. Options: PATCH status, POST /orders/{id}/cancel,
POST /orders/{id}:cancel (Google AIP-136, Azure guidelines) or a cancellation resource.

## Decision
- One-shot state changes use a custom method: POST /orders/{orderId}:cancel.
- Changes with their own lifecycle become resources: POST /orders/{orderId}/returns.
- status is read-only in every representation; it never changes through PUT or PATCH.

## Consequences
- The colon can never appear in our ids (UUIDs, ISBNs and slugs do not contain it).
- Clients that percent-encode the colon (%3A) reach the same endpoint.
- The kebab-case lint rule allows one :customMethod suffix at the end of a path.
