# ADR-0003: The problem type catalog is not paginated

Status: Accepted (2026-11-15)

## Context
BS-30 requires pageSize and pageToken on every list. GET /problems returns the
problem type catalog, which has 16 entries and grows by a few per year.

## Decision
GET /problems returns the whole catalog without pagination. The Spectral rule
bookstore-paginated-lists is turned off for this one operation in .spectral.yaml.

## Consequences
- If the catalog ever needs pages, adding them is a breaking change (AIP-158);
  we accept that for a list we fully control.
