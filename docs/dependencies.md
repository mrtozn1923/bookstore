# Dependencies: Bookstore.Api

What Bookstore needs, what breaks without it, how we see it, and what the recovery path itself depends on.
Reviewed with every postmortem; last review 2026-11-14.

| Dependency | Without it | How we see it | Lever / fallback | The recovery path needs |
|---|---|---|---|---|
| SQL Server (BookstoreDb) | everything fails | checkout SLO pages, /health | none; restore (`deploy/drills/restore-drill.sql`) | backups in a different storage account, a SQL login that is not in the same directory |
| Payment provider | checkout fails | runbook step 3, `http_client_request_duration_seconds` | `Payments:TimeoutSeconds`, `Payments:CheckoutEnabled` | the provider's status page (pinned in `docs/contacts.md`) |
| Prometheus + Alertmanager | no pages | Alertmanager's own watchdog alert at the paging tool | none | paging tool outside our cloud account |
| DNS for bookstore.example.com | nobody reaches us | external synthetic probe | none | registrar login with break-glass account |
| Identity provider (SSO) | staff cannot log in to the cloud console | — | break-glass accounts | `docs/runbooks/break-glass.md` (printed copy in the office safe) |
| Status page | customers do not hear from us | — | post on social accounts | hosted outside our cloud account |
