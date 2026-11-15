# Build from the repository root: docker build -f src/Bookstore.Api/Dockerfile -t bookstore-api:dev .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Bookstore.Api/Bookstore.Api.csproj src/Bookstore.Api/
RUN dotnet restore src/Bookstore.Api/Bookstore.Api.csproj
COPY src/Bookstore.Api/ src/Bookstore.Api/
RUN dotnet publish src/Bookstore.Api/Bookstore.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Bookstore.Api.dll"]
