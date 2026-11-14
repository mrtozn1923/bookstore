# Incident: Payment provider slow, then down (tabletop)

Status: Resolved · Severity: SEV1 · Started: 22:09:47 · Declared: 22:11:47 (2026-11-14, UTC)
IC: Ayse · Ops lead: Emre · Comms lead: Selin · Scribe: Ayse
Tabletop in the lab: fake payment provider, 4 journeys per second (about 228 checkouts a minute). The scenario
steps and the decisions were scripted at fixed minutes; every number below was measured.

## Timeline (UTC, one line per fact or decision)
| Time | Who | What |
|---|---|---|
| 22:09:47 | scenario | Provider answers every charge after 4 s; our payment timeout is 3 s. Every checkout gets 502 after about 3 s. |
| 22:10:30 | Prometheus | CheckoutErrorBudgetBurn (6h, page) fires; 1h page at 22:10:45. |
| 22:11:47 | Ayse | Incident declared, SEV1 (all checkouts fail). Ayse IC, Emre ops lead, Selin comms lead. |
| 22:12 | Emre | Runbook step 3: provider calls end in TaskCanceledException, no status code. Provider is slow, not down. |
| 22:14:47 | Emre | Lever: Payments:TimeoutSeconds 3 → 6. |
| 22:16 | Emre | 0 errors per minute, slowest checkout 4.07 s. Availability back, latency SLO burning. |
| 22:19:47 | scenario | Provider answers 503 to every charge. Checkouts fail in a few milliseconds. |
| 22:21:47 | Emre | Lever: Payments:CheckoutEnabled → false (decided by Ayse: raising the timeout cannot help a 503). |
| 22:22 | Emre | Checkout answers 503 + Retry-After in 1–7 ms; cancelled orders stop growing, carts are kept. |
| 22:25:47 | scenario | Provider healthy again. We did not see it: with the kill switch on, nothing calls the provider. |
| 22:27:47 | Emre | Levers back to default (3 s, enabled). 456 checkouts got 503 while the provider was already healthy. |
| 22:29 | Ayse | Error rate normal for 2 minutes. Incident resolved; postmortem owner: Emre. |

## Levers changed (must be reverted before close)
| Time | Lever | From → To | Reverted at |
|---|---|---|---|
| 22:14:47 | Payments:TimeoutSeconds | 3 → 6 | 22:27:47 |
| 22:21:47 | Payments:CheckoutEnabled | true → false | 22:27:47 |

## Numbers
- Cancelled orders (each one also deleted the customer's cart): 1,598 (1,140 while slow, 457 while down
  before the kill switch).
- Checkouts answered 503 by the kill switch: 1,380; carts kept: 1,370; orders cancelled meanwhile: 0.

## After
- Postmortem needed? Yes (SEV1). Owner: Emre, draft by 2026-11-17.
