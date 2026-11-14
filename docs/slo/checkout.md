# SLO: Checkout

Status: Agreed (2026-11) · Owner: Emre (Bookstore.Api) · Product: Selin · Next review: 2027-02

## Journey
A customer pays for the books in the cart: `POST /api/orders` on Bookstore.Api, which calls the payment provider.
About 1,290 checkouts a day (2026-11).

## SLIs
Source: ASP.NET Core's `http.server.request.duration` histogram, pushed over OTLP to Prometheus
(`deploy/prometheus/rules/sli.yaml`, `slo.yaml`). Route template `/api/orders/`, method `POST`.

| SLI | Valid event | Good event |
|---|---|---|
| Availability | every checkout request except 4xx answers (429 counts as valid) | answered with 2xx or 3xx |
| Latency | the same valid events | finished within 800 ms (bucket `le="0.8"`) |

## Objectives (rolling 30 days)
| SLO | Target | Error budget | At 1,290 checkouts/day |
|---|---|---|---|
| Availability | 99.5 % | 0.5 % | about 194 failed checkouts, or 3.6 hours of full outage |
| Latency | 99 % under 800 ms | 1 % | about 387 checkouts slower than 800 ms |

## Why these numbers
- Measured in November 2026: 100 % availability and 99.7 % of checkouts under 800 ms in a window without incidents;
  the payment provider rejects about 0.1 % of charges, so 99.9 % is the long-run expectation.
  The targets leave room for one bad deploy or a provider hiccup a month.
- Customers complain when the payment page takes more than about a second; 800 ms leaves room for the browser.
- 99.9 % would allow 43 minutes a month; the payment provider's contract promises 99.9 %, so we cannot promise more.

## Not covered (known blind spots)
- Requests that never reach the API (DNS, load balancer, API process down). Planned: a synthetic checkout probe.
- Correctness of the order total and payment amount.
- The institutional bulk order import (a separate SLO is planned).

## Error budget policy
See `error-budget-policy.md`.
