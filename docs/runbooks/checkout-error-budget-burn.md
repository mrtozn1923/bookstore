# Runbook: CheckoutErrorBudgetBurn (page)

Alert rule: `deploy/prometheus/rules/slo-alerts.yaml` · SLO: `docs/slo/checkout.md` · Owner: Bookstore.Api team
Last tried: 2026-11-14 in the lab, with the fake payment provider failing 30 % of charges, and again in the
"payment provider timeouts" tabletop (levers).

## What it means
Checkout (`POST /api/orders`) fails often enough to spend 2 % of the 30-day error budget in an hour
(`long_window=1h`) or 5 % in six hours (`long_window=6h`). Customers cannot pay; every failed checkout is a
cancelled order and its stock is released.

## First 5 minutes
1. Acknowledge the page and write one line in the incident channel: "Looking at CheckoutErrorBudgetBurn".
2. How bad, and which answer? In Prometheus:
   ```promql
   sum by (http_response_status_code) (rate(http_server_request_duration_seconds_count{job="bookstore-api", http_route="/api/orders/"}[5m]))
   ```
3. Is it the payment provider? Bookstore.Api's calls to it, by host and result:
   ```promql
   sum by (server_address, http_response_status_code, error_type) (rate(http_client_request_duration_seconds_count{job="bookstore-api"}[5m]))
   ```
   503, or `error_type="System.Threading.Tasks.TaskCanceledException"` (timeout; such series have no status code)
   from the provider's host: go to "Payment provider".
4. Was there a deploy? Versions that sent metrics in the last hour:
   ```promql
   count by (service_version) (count_over_time(target_info{job="bookstore-api"}[1h]))
   ```
   Two versions, or a version younger than the alert: go to "Bad deploy".
5. Is the process itself healthy? On the host or in the pod, find the PID first (never attach by name: another process
   may have the same name), then watch it:
   ```
   dotnet-counters ps
   dotnet-counters monitor -p <pid> --counters System.Runtime,Microsoft.AspNetCore.Hosting
   ```
   Thread pool queue length growing or CPU near 100 %: go to "Neither".

## Mitigation
### Payment provider
- Check the provider's status page and open a ticket with them (contact: `docs/contacts.md`).
- Do not restart Bookstore.Api: a restart does not fix the provider and drops checkouts in flight.
- Error ratio above 50 % for 15 minutes, or no answer from the provider in 30 minutes: declare an incident
  (`docs/incidents/README.md`).
- Timeouts (`TaskCanceledException`) and the provider still answers slowly: raise `Payments:TimeoutSeconds` in
  `switches.json` above the provider's current latency (`docs/incidents/levers.md`); checkouts succeed again, slowly.
- 503 from the provider, or raising the timeout did not help: `Payments:CheckoutEnabled=false` (503 + Retry-After,
  carts are kept) and ask the product owner for the "payments are delayed" banner on bookstore-web.
- Write every lever change in the incident timeline and set it back when the provider is healthy.

### Bad deploy
- Roll back to the previous version (pipeline: "Redeploy previous release"). Do not debug in production first.
- After the rollback, the 5m error ratio must fall below 7.2 % within 10 minutes; if not, it was not the deploy.

### Neither
- Escalate to the secondary now; do not stay alone with an unknown cause for more than 15 minutes.

## Escalation
| After | Who |
|---|---|
| 0 min | primary on-call (the page) |
| 5 min without acknowledgement | secondary on-call (paging tool) |
| 15 min without a cause or mitigation | secondary joins; consider declaring an incident |
| Payment provider down | provider support line (see `docs/contacts.md`) |

## Afterwards
- Write the timeline in the incident channel while it is fresh.
- More than 20 % of the 30-day budget spent: postmortem (`docs/slo/error-budget-policy.md`).
- If a step in this runbook was wrong or missing, fix it in the same week.
