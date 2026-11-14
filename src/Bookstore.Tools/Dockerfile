# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0

ARG BUILD_CONFIGURATION=Release
ARG APP_VERSION=1.0.0

WORKDIR /src
COPY Bookstore.slnx ./
COPY src/ src/
RUN dotnet publish src/Bookstore.Tools/Bookstore.Tools.csproj -c $BUILD_CONFIGURATION -o /app -p:Version=$APP_VERSION

WORKDIR /app
COPY --chown=app:app data/books.csv data/books.csv

LABEL org.opencontainers.image.title="Bookstore.Tools" \
      org.opencontainers.image.version=$APP_VERSION
ENV DOTNET_ENVIRONMENT=Production \
    Import__File=data/books.csv

USER app
ENTRYPOINT ["dotnet", "Bookstore.Tools.dll"]
CMD ["--Import:DelayMs=500"]
