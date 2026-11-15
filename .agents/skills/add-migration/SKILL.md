---
name: add-migration
description: Create a new EF Core migration in src/Bookstore.Api, review the generated SQL and write rollback notes. Use only when the user explicitly asks for $add-migration with a migration name.
---

# Add an EF Core migration

The user gives the migration name in the prompt, for example `$add-migration AddLowStockAlerts`.

1. If the name is missing or not PascalCase, ask for a name and stop.
2. Build the solution: `dotnet build Bookstore.slnx`. Stop and report if the build fails.
3. Find the latest existing migration: `dotnet ef migrations list --project src/Bookstore.Api`.
4. Create the migration:
   `dotnet ef migrations add <Name> --project src/Bookstore.Api --output-dir Data/Migrations`
5. Generate the SQL from the previous migration to the new one:
   `dotnet ef migrations script <Previous> <Name> --project src/Bookstore.Api --output artifacts/migrations/<Name>.sql`
6. Read the migration and the SQL, then report:
   - tables and columns that are added, changed or dropped
   - anything that can lose data: dropped columns, narrowed types, NOT NULL without a default
   - indexes the new queries need that the migration does not create
7. Write `artifacts/migrations/<Name>.rollback.md` with the previous migration name and the command
   `dotnet ef database update <Previous> --project src/Bookstore.Api`.

Never apply the migration to a database and never edit an existing migration.
