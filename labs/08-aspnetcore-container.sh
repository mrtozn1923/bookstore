#!/usr/bin/env bash
# Docker series 08: Bookstore.Api on .NET 10 images. Run from the repository root.
set -uo pipefail

docker build --check -f src/Bookstore.Api/Dockerfile .
docker build -f src/Bookstore.Api/Dockerfile -t dockerseries-bookstore-api:1.0.0 .

# Image config of the candidate base images
for t in 10.0 10.0-alpine 10.0-azurelinux3.0-distroless 10.0-noble-chiseled 10.0-noble-chiseled-extra 10.0-noble-chiseled-composite; do
    docker image inspect --platform linux/amd64 "mcr.microsoft.com/dotnet/aspnet:$t" \
        --format "$t User={{.Config.User}} Env={{json .Config.Env}}"
done

# Port 8080, non-root, configuration through environment variables
docker run -d --name dockerseries-api -p 8080:8080 --memory 256m --cpus 1 dockerseries-bookstore-api:1.0.0
sleep 5
docker logs dockerseries-api
curl -s localhost:8080/books
curl -s localhost:8080/store/clock
curl -s -o /dev/null -w "openapi: %{http_code}\n" localhost:8080/openapi/v1.json
sleep 15
docker ps --format "table {{.Names}}\t{{.Status}}"
docker rm -f dockerseries-api

docker run -d --name dockerseries-api -p 8080:8080 \
    -e ASPNETCORE_ENVIRONMENT=Development -e Store__TimeZone=UTC dockerseries-bookstore-api:1.0.0
sleep 5
curl -s -o /dev/null -w "openapi: %{http_code}\n" localhost:8080/openapi/v1.json
curl -s localhost:8080/store/clock
docker rm -f dockerseries-api

# What ends up in /app
docker create --name dockerseries-inspect dockerseries-bookstore-api:1.0.0
docker export dockerseries-inspect | tar -tf - | grep "^app/appsettings"
docker rm dockerseries-inspect

docker buildx bake ci
