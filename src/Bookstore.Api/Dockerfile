FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Bookstore.Api/Bookstore.Api.csproj src/Bookstore.Api/
RUN dotnet restore src/Bookstore.Api/Bookstore.Api.csproj
COPY src/Bookstore.Api/ src/Bookstore.Api/
RUN dotnet publish src/Bookstore.Api/Bookstore.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "Bookstore.Api.dll"]

# Optional target for load-test diagnostics: the same image plus dotnet-counters.
# docker compose: set API_TARGET=diag
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS diag-tools
RUN dotnet tool install --tool-path /tools dotnet-counters --version 10.0.*

FROM runtime AS diag
COPY --from=diag-tools /tools /tools
