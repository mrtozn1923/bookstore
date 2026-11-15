# Bookstore

Online bookstore used as the shared example of the mertozen.com series.
.NET 10 Web API in `src/Bookstore.Api`, xUnit v3 integration tests in `tests/Bookstore.Api.Tests`,
Angular 22 client in `web/bookstore-web` (see its own AGENTS.md).

## Commands

- Build: `dotnet build`
- Test: `dotnet test` (needs SQL Server; connection string `ConnectionStrings__BookstoreDb`, see README.md)
- New migration: `dotnet ef migrations add <Name> --project src/Bookstore.Api --output-dir Data/Migrations`

## Rules

<!-- rules:start -->
- Modules live in folders under `src/Bookstore.Api` (Catalog, Ordering, Inventory, Institutional); one SQL schema per module.
- A module never reads another module's tables; it calls that module's service (`ICatalogService`, `IInventoryService`).
- Money is `decimal`, currency `TRY`; round line totals with `MidpointRounding.AwayFromZero`.
- Times are UTC (`TimeProvider`, `DateTime` in UTC).
- Endpoints return `TypedResults` and ProblemDetails for errors; no exceptions for expected outcomes.
- Every behavior change comes with a test; never weaken or delete an existing assertion to make a test pass.
- Never commit secrets or connection strings; use user-secrets or environment variables.
<!-- rules:end -->
