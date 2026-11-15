# Build context: repository root.  docker build -f src/Bookstore.Api/Dockerfile --build-arg APP_VERSION=1.0.0 .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Bookstore.Api/Bookstore.Api.csproj src/Bookstore.Api/
RUN dotnet restore src/Bookstore.Api/Bookstore.Api.csproj
COPY src/Bookstore.Api/ src/Bookstore.Api/
RUN dotnet publish src/Bookstore.Api/Bookstore.Api.csproj -c Release -o /app --no-restore

# -extra: includes ICU, which Microsoft.Data.SqlClient needs (it does not run in globalization invariant mode).
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra
ARG APP_VERSION=dev
ENV APP_VERSION=$APP_VERSION \
    ASPNETCORE_HTTP_PORTS=8080
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Bookstore.Api.dll"]
