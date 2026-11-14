# ADR-0004: Evolving the contract and dated api-versions

Status: Accepted (2026-11-15)

## Context
Book.author is a single string, but some books have several authors (Good Omens).
Replacing it with an array breaks every client that reads author. Institutional
integrations upgrade slowly; bookstore-web upgrades with every release.
Options: never break (keep author forever), URL major version (/v2/books),
a version header or media type, or a dated api-version query parameter (Azure).

## Decision
- Additive changes ship without a version (style guide BS-50).
- Breaking changes ship as a new dated version selected with ?api-version=YYYY-MM-DD,
  as in the Azure guidelines. Unlike Azure the parameter is optional: absent means the
  oldest supported version, so clients that never sent it do not break on release day.
- 2026-11-15 adds authors to every version and removes author from 2026-11-15 onward.
- 2026-09-01 is deprecated on 2026-11-15 and sunsets on 2027-05-17. After the sunset,
  requests without api-version get the oldest version still supported.
- Responses carry Api-Version; deprecated versions add Deprecation, Sunset and a
  Link rel="deprecation" to the version's migration notes.

## Consequences
- The server keeps one representation per supported version for changed resources only.
- Page tokens and caches are not version-specific: versions differ only in representation.
- Clients that never send api-version still break at the sunset; the deprecation
  headers and usage logs are how we find and warn them first.
