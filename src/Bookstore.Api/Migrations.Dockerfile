# EF Core migration bundle for SQL Server. Build context: repository root.
# docker build -f src/Bookstore.Api/Migrations.Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props dotnet-tools.json ./
COPY src/Bookstore.Api/Bookstore.Api.csproj src/Bookstore.Api/
RUN dotnet tool restore && dotnet restore src/Bookstore.Api/Bookstore.Api.csproj
COPY src/Bookstore.Api/ src/Bookstore.Api/
# The design-time connection string only lets the tool build the context; the bundle gets the real one via --connection.
RUN RID=linux-$([ "$TARGETARCH" = "arm64" ] && echo arm64 || echo x64) \
    && ConnectionStrings__BookstoreDb="Server=design-time;Database=BookstoreDb" \
       dotnet ef migrations bundle --project src/Bookstore.Api --self-contained -r $RID -o /out/efbundle

# -extra: includes ICU, which Microsoft.Data.SqlClient needs (it does not run in globalization invariant mode).
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled-extra
COPY --from=build /out/efbundle /efbundle
ENTRYPOINT ["/efbundle"]
