# Postmortem: Payment provider slow, then down (tabletop, 2026-11-14)

Status: Reviewed 2026-11-18 · Severity: SEV1 · Author: Emre · Facilitator: Selin
Incident document: `docs/incidents/2026-11-14-tabletop-payment-provider.md`

## Summary
The payment provider answered every charge after 4 s (our timeout: 3 s) for 10 minutes and then answered 503 for 6
minutes. Every checkout failed in both phases until a lever was used. Raising the timeout restored checkouts within a
minute; the kill switch stopped orders from being cancelled. The kill switch stayed on 2 minutes after the provider
had recovered, because nothing told us it had.

## Impact
- 22:09:47–22:27:47 UTC (18 minutes). Customer-visible: checkout.
- 1,598 orders cancelled; each cancelled order also deleted the customer's cart.
- 1,380 checkouts answered 503 by the kill switch (carts kept), 456 of them while the provider was already healthy.
- 5 minutes of checkouts at about 4 s (latency SLO), 0 errors in that phase.

## Contributing factors
- The payment timeout was fixed at startup; there was no lever until this incident's preparation added one.
- A failed payment cancels the order after the cart has been deleted, so a provider problem costs customers their carts.
- With the kill switch on, Bookstore.Api makes no calls to the provider, so our metrics cannot show its recovery.
- The page does not tell whether a mitigation works: 503s from the kill switch count as failures like 502s.

## What went well
- Runbook step 3 separated "slow" (TaskCanceledException) from "down" (503) within a minute.
- Both levers worked without a restart and were reverted at the end (levers table).

## Where we got lucky
- The provider failed in a way that both levers could handle; a provider that charges but answers late would need
  reconciliation (not covered by any lever).

## Action items
| ID | Action | Class | Owner | Due | Ticket | Status |
|---|---|---|---|---|---|---|
| PM-1 | Keep the cart when a payment fails: delete it only after the payment succeeded | mitigate | Ayse | 2026-11-27 | BOOK-412 | open |
| PM-2 | Synthetic payment probe that also runs while checkout is paused; ticket alert "provider healthy, checkout paused for 5 minutes" | detect | Emre | 2026-11-24 | BOOK-413 | open |
| PM-3 | Runbook: when to raise the timeout and when to use the kill switch; check the provider every 5 minutes while paused | process | Emre | 2026-11-18 | BOOK-414 | done |
| PM-4 | Validate switches.json in the pipeline before it is shipped (keys, types, ranges) | prevent | Selin | 2026-12-01 | BOOK-415 | open |
