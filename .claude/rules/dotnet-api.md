---
paths:
  - "src/Bookstore.Api/**/*.cs"
  - "tests/Bookstore.Api.Tests/**/*.cs"
---

# API rules

- Use file-scoped namespaces and primary constructors where they fit.
- Endpoints live in one static class per module (`CatalogEndpoints`, `OrderingEndpoints`).
- Read-only EF Core queries use `AsNoTracking()`.
- Pass the `CancellationToken` through to every async EF Core call.
- Every new endpoint gets at least one test in `tests/Bookstore.Api.Tests`.
