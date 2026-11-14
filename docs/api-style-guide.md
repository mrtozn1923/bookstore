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

## Methods and status codes
- BS-40 POST to a collection creates: 201, Location and the new representation. A duplicate natural key is 409.
- BS-41 PUT replaces every writable field and may create (201) when the client owns the id (ISBN). A missing field is a validation error, not "keep the old value".
- BS-42 PATCH uses application/merge-patch+json (RFC 7396); other media types get 415 with Accept-Patch. Read-only fields in a patch are 400.
- BS-43 DELETE returns 204; deleting what does not exist is 404 (the effect is idempotent, the status code is not).
- BS-44 400: malformed or schema-invalid request. 422: valid request that a business rule rejects. 409: conflicts with the target resource's current state. 412: If-Match does not match.
- BS-45 Resources the caller may not see answer 404; 403 only when the caller already knows the resource exists.
- BS-46 GET and HEAD never change state.

## Errors
- BS-20 [lint] Every 4xx and 5xx response is application/problem+json (RFC 9457).
- BS-21 type is an absolute URI under https://api.example.com/problems/ and resolves to a documentation page; its last segment is the catalog code (GET /problems). A type never changes meaning; new types may be added.
- BS-22 title is the same for every occurrence of a type and detail describes this occurrence; both are for humans, localized by Accept-Language (en, tr), never parsed by clients.
- BS-23 Validation errors (validation-failed): errors map, keys are JSON paths of the request (price.amount), values are rule codes (required, out-of-range, invalid-length, invalid-format, read-only).
- BS-24 instance is the request path and traceId is always present; no exception messages, stack traces or internal names.
- BS-25 Localized problems carry Content-Language and Vary: Accept-Language.

## Collections
- BS-30 Every list endpoint is paginated from its first version: pageSize, pageToken, nextPageToken.
