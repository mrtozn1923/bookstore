# Build context: repository root.  Image that applies pending PostgreSQL migrations and exits.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Bookstore.Api/Bookstore.Api.csproj src/Bookstore.Api/
COPY src/Bookstore.Migrations.Postgres/Bookstore.Migrations.Postgres.csproj src/Bookstore.Migrations.Postgres/
RUN dotnet restore src/Bookstore.Migrations.Postgres/Bookstore.Migrations.Postgres.csproj
COPY src/ src/
RUN dotnet publish src/Bookstore.Migrations.Postgres -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "Bookstore.Migrations.Postgres.dll"]
