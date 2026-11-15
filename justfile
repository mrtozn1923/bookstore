# Bookstore commands. Run `just` to see the list.

set dotenv-load

[windows]
set shell := ["pwsh", "-NoLogo", "-NoProfile", "-Command"]

web := "web/bookstore-web"
api_port := env("BOOKSTORE_API_PORT", "5080")
web_port := env("BOOKSTORE_WEB_PORT", "4200")

export BOOKSTORE_SEED_FILE := justfile_directory() / "data" / "books.json"

# List all recipes
default:
    @just --list

# Restore .NET tools, NuGet and npm packages
[group('setup')]
restore:
    dotnet tool restore
    dotnet restore
    npm --prefix {{ web }} ci

# First-time setup after scripts/bootstrap (restore + seed data)
[group('setup')]
setup: restore seed

# Build the API and the Angular app
[group('build')]
build:
    dotnet build --no-restore
    npm --prefix {{ web }} run build

# Run .NET and Angular tests
[group('test')]
test:
    dotnet test --no-restore
    npm --prefix {{ web }} test -- --watch=false

# Check formatting without changing files
[group('test')]
format-check:
    dotnet format --verify-no-changes --no-restore
    npm --prefix {{ web }} run format:check

# What CI runs, in the same order
[group('test')]
ci: restore build format-check test

# Start the API and the Angular dev server together
[group('run')]
[parallel]
dev: api web

[private]
api:
    dotnet run --project src/Bookstore.Api --urls http://127.0.0.1:{{ api_port }}

[private]
web:
    npm --prefix {{ web }} start -- --host 127.0.0.1 --port {{ web_port }}

# Write seed data (default: 10 books)
[group('data')]
seed count="10":
    dotnet run tools/seed.cs -- --count {{ count }}

# Delete and rewrite the seed data
[group('data')]
[confirm("Delete data/books.json and write new seed data?")]
db-reset count="10":
    dotnet run tools/seed.cs -- --reset --count {{ count }}
