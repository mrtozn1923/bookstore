# syntax=docker/dockerfile:1.29.0
# check=error=true
ARG DOTNET_VERSION=10.0

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release
ARG APP_VERSION=1.0.0
WORKDIR /src
COPY --parents global.json src/Bookstore.Api/*.csproj ./
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore src/Bookstore.Api/Bookstore.Api.csproj -a $TARGETARCH
# files that only matter on a developer machine never reach the build stage
COPY --exclude=Dockerfile --exclude=Properties/ --exclude=appsettings.Development.json \
    src/Bookstore.Api/ src/Bookstore.Api/
# publish needs no network: every package came from the restore step
RUN --network=none --mount=type=cache,id=nuget,target=/root/.nuget/packages <<EOF
set -eu
dotnet publish src/Bookstore.Api/Bookstore.Api.csproj -c "$BUILD_CONFIGURATION" -a "$TARGETARCH" \
    --no-restore -o /app -p:Version="$APP_VERSION"
test -f /app/Bookstore.Api.dll
test ! -e /app/appsettings.Development.json
EOF

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-noble-chiseled-extra AS final
ARG APP_VERSION=1.0.0
ARG GIT_SHA=unknown
WORKDIR /app
COPY --link --from=build /app ./
LABEL org.opencontainers.image.title="Bookstore.Api" \
      org.opencontainers.image.version=$APP_VERSION \
      org.opencontainers.image.revision=$GIT_SHA
EXPOSE 8080
USER $APP_UID
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD ["dotnet", "Bookstore.Api.dll", "healthcheck"]
ENTRYPOINT ["dotnet", "Bookstore.Api.dll"]
