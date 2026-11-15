# Bookstore

Online bookstore: ASP.NET Core 10 API, Angular 22 client, SQL Server.
See README.md for local setup.

## Build and test
- Build: `dotnet build Bookstore.slnx`
- API tests: `dotnet test` (needs SQL Server; the connection string `ConnectionStrings:BookstoreDb` is in user-secrets)
- Client: run `npm test` and `npm run build` in `web/bookstore-web`
- Run the relevant tests before you say a change is done, and show the test summary.

## Layout
- `src/Bookstore.Api`: endpoints, EF Core `BookstoreContext`, migrations in `src/Bookstore.Api/Data/Migrations`
- `src/Bookstore.Contracts`: DTOs shared with the client; no reference to EF Core
- `tests/Bookstore.Api.Tests`: API tests
- `web/bookstore-web`: Angular client with its own `AGENTS.md`

## Rules
- Money is `decimal` with currency `TRY`; times are stored in UTC.
- Endpoints return `ProblemDetails` on errors.
- Read-only EF Core queries use `AsNoTracking()` and pass the `CancellationToken` through.
- Never edit an existing migration; add a new migration instead (see the `add-migration` skill).
- Never put secrets in code or appsettings files; use user-secrets locally.
- Ask before adding a NuGet or npm package.

## Code Review Rules
### EF Core queries
- Flag queries that load related data inside a loop (N+1). Safe path: `Include`, a projection with `Select`, or one query with a join.
- Flag read-only queries without `AsNoTracking()` and async EF Core calls that drop the `CancellationToken`.
- Flag list endpoints that return an unbounded result set. Safe path: paging with `Skip` and `Take`, as in `GET /orders`.
### Money and time
- Flag money stored or calculated as `double` or `float`, and times stored as local time instead of UTC.
