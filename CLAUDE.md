# Bookstore

Online bookstore: ASP.NET Core 10 API, Angular 22 client, SQL Server.
See @README.md for local setup.

<!-- Maintainers: keep this file under 100 lines. Area rules live in .claude/rules/. -->

## Build and test
- Build: `dotnet build Bookstore.slnx`
- API tests: `dotnet test` (needs SQL Server; the connection string `ConnectionStrings:BookstoreDb` is in user-secrets)
- Client: `npm test` and `npm run build` in `web/bookstore-web`
- Run the relevant tests before you say a change is done, and show the test summary.

## Layout
- `src/Bookstore.Api`: endpoints, EF Core `BookstoreContext`, migrations in `src/Bookstore.Api/Data/Migrations`
- `src/Bookstore.Contracts`: DTOs shared with the client; no reference to EF Core
- `tests/Bookstore.Api.Tests`: API tests
- `web/bookstore-web`: Angular client

## Rules
- Money is `decimal` with currency `TRY`; times are stored in UTC.
- Endpoints return `ProblemDetails` on errors.
- Never edit an existing migration; add a new migration instead.
- Never put secrets in code or appsettings files; use user-secrets locally.
- Ask before adding a NuGet or npm package.

## Compact Instructions
When compacting, keep: the approved plan and which of its steps are done,
the exact test commands and their last results, and every rule I gave in
this session that is not already in this file.
