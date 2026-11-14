# Bookstore HTTP API style guide

Each rule has an id. Rules marked [lint] are enforced by .spectral.yaml.

## Resources and URLs
- BS-01 [lint] Path segments are plural nouns in lowercase kebab-case; path parameters are camelCase.
- BS-02 [lint] No CRUD verbs in paths; the HTTP method is the verb.

## Representations
- BS-10 JSON property names are camelCase.
- BS-11 Absent values are omitted, never sent as null.
- BS-12 Money is an object: { "amount": 489.00, "currency": "TRY" }.
- BS-13 Internal fields (stock counts, supplier codes, database keys) are not exposed.

## Errors
- BS-20 Every 4xx and 5xx response is application/problem+json (RFC 9457).

## Collections
- BS-30 Every list endpoint is paginated from its first version: pageSize, pageToken, nextPageToken.
