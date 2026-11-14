# Game day: payment provider errors (2026-11-14, lab)

Owner: Ayse · IC: Ayse · Ops lead: Emre · Comms lead: Selin · Environment: lab (Development), never Production

## Steady state (measured before the start)
- Checkout availability (5m error ratio, `slo:checkout_availability:error_ratio_rate5m`) below 0.5 %.
- Checkout latency: at least 99 % under 800 ms.

## Hypotheses
1. With 10 % of payment calls answered 503 and 5 % delayed by 1 s, checkout fails about 10 % of the time and the
   1h CheckoutErrorBudgetBurn page fires within 5 minutes.
2. The on-call finds the cause with runbook step 3 within 5 minutes.
3. Turning chaos off (Chaos:Enabled=false in switches.json) stops the failures within a minute.

## Injection
`switches.json`: `"Chaos": { "Enabled": true, "FaultRate": 0.1, "LatencyRate": 0.05, "LatencyMs": 1000 }` for 6 minutes,
4 user journeys per second of synthetic traffic. Blast radius: the lab only; real customers never.

## Abort conditions (anyone may call them; the script also checks the first one every 15 s)
- 5m checkout error ratio above 20 %.
- Any page other than CheckoutErrorBudgetBurn.
- Anyone in the room is unsure what is happening.
Abort = `"Chaos": { "Enabled": false }` in switches.json; no restart needed.

## Results
| Run | Pipeline | Checkout errors (5m) | Latency under 800 ms | Pages | Found |
|---|---|---|---|---|---|
| 1 (23:02–23:08) | chaos only | 9.3 % | 95.9 % | none from the 1h window | H1 false: at burn rate 18.6 the 1h page needs about 46 minutes. H2 false: step 3 showed only 200s, the page said "No failing outgoing calls"; the chaos answers never leave the process. H3 true: abort took effect within 30 s. |
| 2 (23:16–23:21) | retry once on 503 + chaos | 0.9 % | 93.5 % | none | The retry turned 10 % provider errors into 0.9 % checkout errors; it costs latency (each attempt can be delayed). |

## Action items
| ID | Action | Class | Owner | Due | Ticket | Status |
|---|---|---|---|---|---|---|
| GD-1 | Runbook step 3: look at Polly's resilience events when outgoing calls look healthy | detect | Emre | 2026-11-15 | BOOK-421 | done |
| GD-2 | One retry for a 503 from the provider (Idempotency-Key makes it safe) | mitigate | Ayse | 2026-11-15 | BOOK-422 | done |
| GD-3 | Error budget policy: write down how long a 10 % error rate takes to page (about 46 minutes) and decide if that is acceptable | process | Selin | 2026-11-28 | BOOK-423 | open |
| GD-4 | Repeat this game day in three months with a circuit breaker in the pipeline | process | Ayse | 2027-02-15 | BOOK-424 | open |
