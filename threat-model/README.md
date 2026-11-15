# Threat model

`bookstore-checkout.json` is an OWASP Threat Dragon 2.6 model (JSON, schema version 2.x).
Open it with the Threat Dragon desktop app (File > Open) or the web app (Login to Local Session >
Open an existing threat model > paste or drop the file > Import). Save it back to this folder and commit it
together with the code change that made it necessary.

## When the model is updated

- A new feature changes a data flow, adds a data store or crosses a trust boundary.
- A new third-party integration (payment, shipping, e-mail, identity provider) is added or replaced.
- An architecture change: a new service, queue, cache or deployment zone.
- A security incident or pentest finding shows that the model was wrong.

## Scope of the current model

| Diagram | Scope | Last review |
|---|---|---|
| Checkout, Payments and Shipping | Customer checkout, hosted payment page, provider webhook, shipment | 2026-11-15 |
| Institutional purchase approval | Purchase request, approval above the unit limit, institutional order | 2026-11-15 |

Out of scope for now: back office, Catalog administration, Notifications.

## Threats

Threats are found per element with STRIDE and scored as likelihood (1–3) x impact (1–3):
6–9 High, 3–5 Medium, 1–2 Low. Threat Dragon has no "Accepted" status:
an accepted threat stays `Open` and points to its row in `risk-register.md`.
Every threat ends in one of: a test (`[Trait("threat", "T09")]`), a backlog item (SEC-nn),
or a row in `risk-register.md`. A threat is `Mitigated` only when its test exists and passes.
