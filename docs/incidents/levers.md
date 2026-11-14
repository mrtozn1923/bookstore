# Incident levers: Bookstore.Api

Things the ops lead can change during an incident without a deploy. Every lever is tried in the lab before it is
listed here (last tried: 2026-11-14, tabletop "payment provider timeouts").

## switches.json (no restart)
Bookstore.Api watches `switches.json` next to the app (path: `SwitchesFile` setting) and applies changes within
seconds. It wins over appsettings*.json and environment variables.

```json
{ "Payments": { "TimeoutSeconds": 3, "CheckoutEnabled": true } }
```

| Lever | When | Effect | Cost |
|---|---|---|---|
| `Payments:TimeoutSeconds` (1–30, default 3) | the provider is slow but answers | checkouts succeed again | every checkout waits as long as the provider; the latency SLO burns |
| `Payments:CheckoutEnabled=false` | the provider fails, raising the timeout does not help | 503 + `Retry-After: 300` before an order is created; carts and stock are kept | no sales; the availability SLO still burns |

Put every lever back to its default when the incident ends, and write the change and the time in the timeline.

## Other levers
- Roll back to the previous release: pipeline "Redeploy previous release" (Deployment Strategies series).
- Banner on bookstore-web: "Payments are delayed" (product owner).
