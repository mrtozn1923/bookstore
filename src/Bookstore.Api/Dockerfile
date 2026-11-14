# syntax=docker/dockerfile:1
ARG DOTNET_VERSION=10.0

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release
ARG APP_VERSION=1.0.0
WORKDIR /src
COPY global.json ./
COPY src/Bookstore.Api/Bookstore.Api.csproj src/Bookstore.Api/
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore src/Bookstore.Api/Bookstore.Api.csproj -a $TARGETARCH
COPY src/Bookstore.Api/ src/Bookstore.Api/
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish src/Bookstore.Api/Bookstore.Api.csproj -c $BUILD_CONFIGURATION -a $TARGETARCH \
        --no-restore -o /app -p:Version=$APP_VERSION

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-noble-chiseled-extra AS final
ARG APP_VERSION=1.0.0
WORKDIR /app
COPY --from=build /app ./
LABEL org.opencontainers.image.title="Bookstore.Api" \
      org.opencontainers.image.version=$APP_VERSION
EXPOSE 8080
USER $APP_UID
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD ["dotnet", "Bookstore.Api.dll", "healthcheck"]
ENTRYPOINT ["dotnet", "Bookstore.Api.dll"]
