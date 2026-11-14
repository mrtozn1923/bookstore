# AGENTS.md — Bookstore

Online bookstore: ASP.NET Core 10 Web API, EF Core 10 on SQL Server, Angular 22 client.

## Commands
- Build: `dotnet build` (warnings are errors, see `Directory.Build.props`)
- Test: `dotnet test` (needs SQL Server and `ConnectionStrings__BookstoreDb`; tests create `<db>_Tests`)
- Run the API: `dotnet run --project src/Bookstore.Api` (http://localhost:5080)
- Angular client: see `web/bookstore-web/AGENTS.md`

## Layout
- `src/Bookstore.Api/<Module>/`: one folder per module (Catalog, Ordering, Inventory).
  `<Module>Module.cs` holds `Add<Module>Module()` and `Map<Module>Endpoints()`.
- `src/Bookstore.Api/Data/`: `BookstoreContext` and migrations.
- `tests/Bookstore.Api.Tests/`: integration tests, xUnit v3 + `WebApplicationFactory`.

## Rules
- Minimal APIs with route groups and `TypedResults`. No MVC controllers, no `Startup.cs`.
- A module reads another module's data only through its interface (`ICatalogService`, `ICartService`, `IInventoryService`).
- Money is `decimal` in TRY; times are UTC and come from `TimeProvider`.
- Schema change: edit the entity and its configuration, then
  `dotnet ef migrations add <Name> --project src/Bookstore.Api`. Never edit an existing migration.
- New behaviour comes with an integration test. Do not delete, skip or weaken an existing test.
- Do not add NuGet or npm packages without asking. Versions live in `Directory.Packages.props`.
  Check a new package id first: `dotnet run eng/check-packages.cs -- <PackageId>`.
- No secrets in files. Connection strings come from user-secrets or environment variables;
  never print them (`dotnet user-secrets list`, `env`) into the chat or a commit.

## Ask first
Changing a public endpoint contract, touching migrations or CI files, anything you are not sure about.
