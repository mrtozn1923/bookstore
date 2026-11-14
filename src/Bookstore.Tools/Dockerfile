# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0

ARG BUILD_CONFIGURATION=Release
ARG APP_VERSION=1.0.0

WORKDIR /src
# Rarely changing files first: solution and project files
COPY Bookstore.slnx ./
COPY src/Bookstore.Tools/Bookstore.Tools.csproj src/Bookstore.Tools/
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore src/Bookstore.Tools/Bookstore.Tools.csproj

# Source code changes often: copy it after the restore layer
COPY src/ src/
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish src/Bookstore.Tools/Bookstore.Tools.csproj -c $BUILD_CONFIGURATION -o /app \
        --no-restore -p:Version=$APP_VERSION

WORKDIR /app
COPY --chown=app:app data/books.csv data/books.csv

LABEL org.opencontainers.image.title="Bookstore.Tools" \
      org.opencontainers.image.version=$APP_VERSION
ENV DOTNET_ENVIRONMENT=Production \
    Import__File=data/books.csv

USER app
ENTRYPOINT ["dotnet", "Bookstore.Tools.dll"]
CMD ["--Import:DelayMs=500"]
