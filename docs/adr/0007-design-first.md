# ADR-0007: Design-first with the OpenAPI contract

Status: Accepted (2026-11-15)

## Context
Until now the contract was written by hand next to the code, and nothing checked that
the two matched: /legacy/rpc existed in the code but not in the contract. Partners ask
for a contract weeks before an endpoint ships so they can start their integration.

## Decision
- contract/bookstore.openapi.yaml (OpenAPI 3.2) is the source of the design; a change
  is reviewed there before code is written.
- CI runs Redocly CLI and Spectral (style guide), oasdiff breaking (against the target
  branch, failing on warnings too because unknown query parameters are 400), openapi-typescript (client freshness) and a drift check that compares the
  contract's operations with the document ASP.NET Core generates from the code.
- Partners and bookstore-web get a Prism mock server of the contract for early work.

## Consequences
- Tools lag behind OpenAPI 3.2: Spectral 6.17.1 skips the QUERY operation, Prism 5.16
  does not mock it, openapi-typescript 7.13 needs its structural check turned off and
  generates no type for it, and ASP.NET Core 10 generates OpenAPI 3.1.1 without it.
  QUERY /books is an allowed difference in the drift check.
- The generated document is a test artifact, not the published contract.
