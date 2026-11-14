# ADR-0001: Bookstore HTTP API style guide

Status: Accepted (2026-11-15)

## Context
Bookstore.Api has three kinds of consumers: bookstore-web (Angular, our team),
institutional integrations (school and library systems, other teams we do not control)
and a planned mobile app. The old /legacy/rpc endpoint leaks table columns and returns
200 for every error; partners already depend on it. ASP.NET Core and Angular both
default to camelCase JSON and ASP.NET Core produces RFC 9457 Problem Details.

## Decision
We write our own short style guide (docs/api-style-guide.md) and base it on the
Microsoft Azure REST API Guidelines, with two exceptions:
- errors use RFC 9457 Problem Details, not the Azure error object;
- pagination follows Google AIP-158 (pageSize, pageToken, nextPageToken).
Rules that a tool can check are enforced by .spectral.yaml in CI; the rest are
checked in API review.

## Consequences
- New endpoints are designed in contract/bookstore.openapi.yaml before code.
- /legacy/rpc is frozen; no new methods are added to it.
- Deviations from the guide need a new ADR.
