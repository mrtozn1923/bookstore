#!/usr/bin/env bash
# Docker series 05: the first Dockerfile for Bookstore.Tools. Run from the repository root.
set -uo pipefail

# Build checks on the first draft (exit code 1 when warnings are found)
docker build --check -f labs/05-build-check/Dockerfile.first .
echo "check exit code: $?"

# Build and run the real image
docker build --check -f src/Bookstore.Tools/Dockerfile .
docker build -f src/Bookstore.Tools/Dockerfile -t dockerseries-bookstore-tools:1.0.0 .
docker history dockerseries-bookstore-tools:1.0.0
docker image inspect dockerseries-bookstore-tools:1.0.0 --format "Entrypoint={{json .Config.Entrypoint}} Cmd={{json .Config.Cmd}} User={{.Config.User}}"
docker run --rm dockerseries-bookstore-tools:1.0.0
docker run --rm dockerseries-bookstore-tools:1.0.0 --Import:BatchSize=5
docker run --rm -e Import__File=data/missing.csv dockerseries-bookstore-tools:1.0.0
echo "exit code: $?"
docker run --rm --entrypoint ls dockerseries-bookstore-tools:1.0.0 -ln data

# ARG values end up in the image history
docker build --build-arg NUGET_TOKEN=fake-token-for-demo -t dockerseries-argsecret labs/05-arg-secret
docker history --no-trunc --format "{{.CreatedBy}}" dockerseries-argsecret | head -4

# EXPOSE documents a port, it does not publish it
docker run -d --name dockerseries-web nginx:1.30-alpine
sleep 1
docker port dockerseries-web
curl -sS -m 3 localhost:80
docker rm -f dockerseries-web
docker run -d -P --name dockerseries-web nginx:1.30-alpine
sleep 1
docker port dockerseries-web
docker rm -f dockerseries-web

# apt-get update and install in one RUN
docker build -t dockerseries-aptcache labs/05-apt-cache
docker run --rm dockerseries-aptcache jq --version

docker rmi dockerseries-argsecret dockerseries-aptcache
