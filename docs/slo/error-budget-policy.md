# Error budget policy: Bookstore.Api

Status: Agreed (2026-11) · Applies to: every SLO in `docs/slo/` · Owner: Selin (Product Owner) and Emre (Bookstore.Api)

## Why
The SLOs say how unreliable checkout may be. This policy says what the team does with that room, decided
before an incident, not during one. It is not a punishment: it tells us when reliability work comes first.

## How the budget is measured
- Rolling 30-day window, from the Prometheus rules in `deploy/prometheus/rules/`.
- Remaining budget = 1 - (bad events / allowed bad events). 100 % = untouched, 0 % = used up, below 0 = SLO missed.
- Every failure counts, including the payment provider's: customers do not care whose fault it was.

## What we do
| Remaining budget (30 days) | Releases | Work |
|---|---|---|
| above 50 % | normal release cadence | normal |
| 0–50 % | normal, but risky changes (schema, payment flow) need a rollback plan in the PR | look at what spent the budget |
| below 0 (SLO missed) | only fixes for the SLO, security fixes and data fixes until back above 0 | the next sprint starts with reliability items |

## Single incidents
- An incident that uses more than 20 % of a 30-day budget gets a postmortem with at least one action item
  that prevents or shortens the same failure.
- If the payment provider spent the budget, the action items are on our side (timeouts, fallback, status
  messages to the customer, a talk with the provider), not "wait for the provider".

## Who decides
The on-call developer reports the budget in the weekly review. Selin and Emre decide together; if they
disagree, the engineering manager decides within two working days.

## Review
This policy and the SLO targets are reviewed every quarter, together with the last quarter's incidents.
