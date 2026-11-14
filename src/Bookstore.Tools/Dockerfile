# syntax=docker/dockerfile:1
ARG DOTNET_VERSION=10.0

# restore: SDK runs on the build machine's platform, never under QEMU
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS restore
WORKDIR /src
COPY Bookstore.slnx global.json ./
COPY src/Bookstore.Tools/Bookstore.Tools.csproj src/Bookstore.Tools/
COPY tests/Bookstore.Tools.Tests/Bookstore.Tools.Tests.csproj tests/Bookstore.Tools.Tests/
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore Bookstore.slnx
COPY src/ src/

# build: publish for the target architecture (cross-compilation)
FROM restore AS build
ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release
ARG APP_VERSION=1.0.0
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish src/Bookstore.Tools/Bookstore.Tools.csproj -c $BUILD_CONFIGURATION -a $TARGETARCH \
        -o /app -p:Version=$APP_VERSION

# test: only built when asked for (--target test or the bake "test" target)
FROM restore AS test
COPY tests/ tests/
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet test --project tests/Bookstore.Tools.Tests/Bookstore.Tools.Tests.csproj --no-restore

# export: publish output to the host (docker build --target export --output out)
FROM scratch AS export
COPY --from=build /app /

# runtime: runtime only, no SDK, no shell, non-root
FROM mcr.microsoft.com/dotnet/runtime:${DOTNET_VERSION}-noble-chiseled AS runtime
ARG APP_VERSION=1.0.0
WORKDIR /app
COPY --from=build /app ./
COPY --chown=app:app data/books.csv data/books.csv
LABEL org.opencontainers.image.title="Bookstore.Tools" \
      org.opencontainers.image.version=$APP_VERSION
ENV DOTNET_ENVIRONMENT=Production \
    Import__File=data/books.csv
USER app
ENTRYPOINT ["dotnet", "Bookstore.Tools.dll"]
CMD ["--Import:DelayMs=500"]

# debug: runtime plus a static busybox taken from another image
FROM runtime AS debug
COPY --from=busybox:1.37-musl /bin/busybox /busybox/busybox

# final: the last stage is the default target, so it must be the production image
FROM runtime AS final
