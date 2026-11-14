# Bookstore HTTP API style guide

Each rule has an id. Rules marked [lint] are enforced by .spectral.yaml.

## Resources and URLs
- BS-01 [lint] Path segments are plural nouns in lowercase kebab-case; path parameters are camelCase.
- BS-02 [lint] No CRUD verbs in paths; the HTTP method is the verb.
- BS-03 [lint] A one-shot state change that is not CRUD is a custom method: POST /{collection}/{id}:{verb} (ADR-0002).
- BS-04 Something with its own lifecycle or approval (a return, an approval request) is a resource, not a custom method.
- BS-05 [lint] At most two path parameters (one level of nesting); deeper hierarchies are query filters.
- BS-06 Every resource has exactly one canonical URL; other collections that list it are filtered views.
- BS-07 Ids: ISBN-13 for books, UUID v7 for orders and returns, lowercase slugs for institutions and units; "me" is an alias for the caller.

## Representations
- BS-10 JSON property names are camelCase.
- BS-11 Absent values are omitted, never sent as null.
- BS-12 Money is an object: { "amount": 489.00, "currency": "TRY" }.
- BS-13 Internal fields (stock counts, supplier codes, database keys) are not exposed.

## Errors
- BS-20 Every 4xx and 5xx response is application/problem+json (RFC 9457).

## Collections
- BS-30 Every list endpoint is paginated from its first version: pageSize, pageToken, nextPageToken.
