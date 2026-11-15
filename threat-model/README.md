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

Out of scope for now: back office, Catalog administration, Notifications, Institutional orders.
