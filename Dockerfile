# Stage 1: build and publish in the SDK image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# csproj first: the restore layer comes from the cache even when the code changes
COPY src/Bookstore.Api/Bookstore.Api.csproj src/Bookstore.Api/
RUN dotnet restore src/Bookstore.Api/Bookstore.Api.csproj

# then the rest of the code
COPY src/ src/
RUN dotnet publish src/Bookstore.Api/Bookstore.Api.csproj -c Release -f net6.0 -o /app/publish --no-restore

# Stage 2: a small image with only the runtime
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Bookstore.Api.dll"]
