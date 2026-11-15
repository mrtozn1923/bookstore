# Build context: repository root.  docker build -f src/Bookstore.Api/Dockerfile -t <image>:<tag> --build-arg APP_VERSION=<tag> --build-arg SOURCE_COMMIT=<sha> .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Bookstore.Api/Bookstore.Api.csproj src/Bookstore.Api/
RUN dotnet restore src/Bookstore.Api/Bookstore.Api.csproj
COPY src/Bookstore.Api/ src/Bookstore.Api/
RUN dotnet publish src/Bookstore.Api/Bookstore.Api.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled
ARG APP_VERSION=dev
ARG SOURCE_COMMIT=unknown
ENV APP_VERSION=$APP_VERSION SOURCE_COMMIT=$SOURCE_COMMIT
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Bookstore.Api.dll"]
