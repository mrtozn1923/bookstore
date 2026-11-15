---
name: add-migration
description: Create a new EF Core migration in Bookstore.Api, review the generated SQL and write rollback notes. Run only when the user invokes /add-migration.
argument-hint: "[MigrationName]"
arguments: [name]
disable-model-invocation: true
allowed-tools:
  - Bash(dotnet build Bookstore.slnx)
  - Bash(dotnet ef migrations list *)
  - Bash(dotnet ef migrations add *)
  - Bash(dotnet ef migrations script *)
  - PowerShell(dotnet build Bookstore.slnx)
  - PowerShell(dotnet ef migrations list *)
  - PowerShell(dotnet ef migrations add *)
  - PowerShell(dotnet ef migrations script *)
---

# Add an EF Core migration

Migration name: $name

1. If the name is empty or not PascalCase (for example `AddLowStockAlerts`), ask for a name and stop.
2. Build the solution: `dotnet build Bookstore.slnx`. Stop and report if the build fails.
3. Find the latest existing migration: `dotnet ef migrations list --project src/Bookstore.Api`.
4. Create the migration:
   `dotnet ef migrations add $name --project src/Bookstore.Api --output-dir Data/Migrations`
5. Generate the SQL from the previous migration to the new one:
   `dotnet ef migrations script <Previous> $name --project src/Bookstore.Api --output artifacts/migrations/$name.sql`
6. Read the migration and the SQL, then report:
   - tables and columns that are added, changed or dropped
   - anything that can lose data: dropped columns, narrowed types, NOT NULL without a default
   - indexes the new queries need that the migration does not create
7. Write `artifacts/migrations/$name.rollback.md` with the previous migration name and the command
   `dotnet ef database update <Previous> --project src/Bookstore.Api`.

Never apply the migration to a database and never edit an existing migration.
