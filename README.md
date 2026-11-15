# Bookstore.Analytics — "Analytics ve Ölçüm" series code

Every dataset under `data/` is synthetic, made for the examples; none of it is real site data.

## Requirements
- .NET 10 SDK

## Build and test
    dotnet build
    dotnet test

## Commands
    # 01: classify an access log (combined format) by traffic source
    dotnet run --project src/Bookstore.Analytics.Cli -- logs data/access-sample.log www.bookstore.example
