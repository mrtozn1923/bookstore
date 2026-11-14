# Bookstore API review checklist

Used on every pull request that changes contract/bookstore.openapi.yaml, before code is written.
Rules with [lint] or [ci] are checked by tools; the reviewer reads the tool output and checks the rest.

| Area | Question | Rules |
|---|---|---|
| Consumers | Who calls this (bookstore-web, partners, mobile)? Is it public, partner or internal? | ADR-0001 |
| Resources | Nouns from the domain, one canonical URL, at most two path parameters, custom method or resource? | BS-01..07 [lint] |
| Representation | camelCase, absent instead of null, Money objects, no internal fields, open response enums? | BS-10..13, BS-52 |
| Methods and codes | Safe and idempotent methods used as such; 201/202/204/400/409/412/422/428 as in the guide? | BS-40..46 |
| Errors | Problem Details with catalog types; new types added to the catalog; validation keys as JSON paths? | BS-20..25 [lint] |
| Collections | Paginated from version one, documented sort and filters, unknown parameters 400? | BS-30..37 [lint] |
| Evolution | Additive? If oasdiff reports a breaking change: new api-version or an ADR? Deprecations explained? | BS-50..55, BS-82 [ci] |
| Concurrency | Changes to existing resources need If-Match; creates with side effects need Idempotency-Key? | BS-60..62 |
| Long work and bulk | 202 with a status monitor? Size limit, per-line report, quota? | BS-63..76 [lint] |
| Security | Identity from the token, never from the body; authorization per item; nothing about other tenants leaks? | API Security series |
| Documentation | Descriptions, realistic examples per status code, webhooks described? | BS-83 |
| Decisions | Exceptions to the guide recorded as ADRs? Open questions listed? | ADR-0001 |
