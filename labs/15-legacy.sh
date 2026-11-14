#!/usr/bin/env bash
# Docker series 15: reading a 2021 Dockerfile on Engine 29. Run from the repository root.
set -uo pipefail

echo "### Legacy builder and build checks"
DOCKER_BUILDKIT=0 docker build -f labs/15-legacy/Dockerfile.2021 -t dockerseries-bookstore-api:2021 src/Bookstore.Api
docker build --check -f labs/15-legacy/Dockerfile.2021 src/Bookstore.Api

echo "### Port, user and OS of the .NET images"
for t in core/aspnet:3.1 aspnet:6.0 aspnet:7.0 aspnet:8.0 aspnet:9.0 aspnet:10.0; do
    printf "%s " $t
    docker buildx imagetools inspect mcr.microsoft.com/dotnet/$t \
        --format '{{json (index .Image "linux/amd64").Config}}' \
        | jq -c '{User, Env: [.Env[] | select(test("ASPNETCORE_|APP_UID"))]}'
done
for t in 9.0 10.0; do
    printf "aspnet:%s " $t
    docker run --rm --entrypoint cat mcr.microsoft.com/dotnet/aspnet:$t /etc/os-release | grep PRETTY
done

echo "### Engine 29: image store and API version"
docker info | grep -A1 "Storage Driver"
DOCKER_API_VERSION=1.39 docker version
DOCKER_API_VERSION=1.43 docker version --format "server {{.Server.Version}} api {{.Server.APIVersion}} min {{.Server.MinAPIVersion}}"

echo "### .NET 11 SDK"
bash labs/15-legacy/net11-container.sh

docker rmi mcr.microsoft.com/dotnet/core/sdk:3.1 mcr.microsoft.com/dotnet/aspnet:9.0 mcr.microsoft.com/dotnet/aspnet:10.0
