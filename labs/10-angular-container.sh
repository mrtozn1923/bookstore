#!/usr/bin/env bash
# Docker series 10: bookstore-web (Angular 22) on unprivileged nginx with runtime configuration. Run from the repository root.
set -uo pipefail

docker build --check web
docker build -t dockerseries-bookstore-web:1.0.0 web
docker build -f src/Bookstore.Api/Dockerfile -t dockerseries-bookstore-api:1.0.0 .

# The same web image, configured at startup
docker run -d --name dockerseries-api -p 8080:8080 \
    -e Cors__AllowedOrigins__0=http://localhost:8081 dockerseries-bookstore-api:1.0.0
docker run -d --name dockerseries-web -p 8081:8080 \
    -e API_BASE_URL=http://localhost:8080 -e APP_ENVIRONMENT=staging dockerseries-bookstore-web:1.0.0
sleep 5
docker logs dockerseries-web
curl -s localhost:8081/config.json
curl -s -o /dev/null -D - localhost:8081/books/9780441172719 | grep -iE "^HTTP|cache-control"
curl -s -o /dev/null -D - localhost:8081/config.json | grep -iE "^HTTP|cache-control"
curl -s -o /dev/null -D - -H "Origin: http://localhost:8081" localhost:8080/books | grep -i access-control
docker exec dockerseries-web id
docker rm -f dockerseries-web dockerseries-api

# Fail fast without configuration
docker run --rm dockerseries-bookstore-web:1.0.0
echo "exit code: $?"

docker buildx bake ci
