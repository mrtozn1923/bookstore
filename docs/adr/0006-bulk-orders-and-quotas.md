# ADR-0006: Bulk orders, partial success and quotas

Status: Accepted (2026-11-15)

## Context
Schools send their yearly reading lists as one bulk order of up to 500 lines.
Some lines are always unknown books or out of stock. Integrations used to loop over
POST /orders, one request per line, and one school's loop slowed the API for everyone.

## Decision
- POST /institutions/{institutionId}/order-batches takes up to 500 lines for one unit.
- Schema errors reject the whole request (400); business rules are checked per line.
  Accepted lines become one Pending order; the response is 201 with a report that has
  one result per line. allOrNothing turns any rejected line into a 422 without an order.
- We do not use 207 Multi-Status: the WebDAV status codes do not describe our lines, and
  a 2xx with a report is easier to handle in every client.
- Prefer: respond-async starts the same batch as a long-running operation (ADR-0005).
- Quota: 5 batches per minute per institution, announced with the RateLimit-Policy and
  RateLimit fields of draft-ietf-httpapi-ratelimit-headers-11. The draft is not an RFC
  and its I-D expires on 2026-11-24; we review the field format when it moves.
- Completion webhooks (order-batch.completed) are described in the contract and planned;
  until they are sent, clients poll the operation.

## Consequences
- Clients must read the report; a 201 does not mean every line was ordered.
- The quota counters are per instance in the sample; several instances need a shared
  store or a limit at the gateway.
