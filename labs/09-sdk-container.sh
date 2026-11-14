#!/usr/bin/env bash
# Docker series 09: containers without a Dockerfile (dotnet publish /t:PublishContainer). Run from the repository root.
# Needs the .NET 10 SDK on the machine and Docker for the local-daemon and registry steps.
set -uo pipefail

# Local Docker daemon (ContainerRepository, ContainerImageTags and ContainerFamily come from the csproj)
dotnet publish src/Bookstore.Api/Bookstore.Api.csproj -t:PublishContainer
docker image inspect dockerseries-bookstore-api:1.0.0-sdk \
    --format 'User={{.Config.User}} Entrypoint={{json .Config.Entrypoint}} Ports={{json .Config.ExposedPorts}}'
docker run -d --name dockerseries-api -p 8080:8080 dockerseries-bookstore-api:1.0.0-sdk
sleep 5
curl -s localhost:8080/store/clock
docker rm -f dockerseries-api

# Tarball, no daemon needed while building
dotnet publish src/Bookstore.Api/Bookstore.Api.csproj -t:PublishContainer -p ContainerArchiveOutputPath="$PWD/out/images/"
docker load -i out/images/dockerseries-bookstore-api.tar.gz
rm -rf out

# Straight to a registry, multi-architecture, OCI format
docker run -d --name dockerseries-registry -p 5000:5000 registry:3
dotnet publish src/Bookstore.Api/Bookstore.Api.csproj -t:PublishContainer \
    -p ContainerRegistry=localhost:5000 \
    -p RuntimeIdentifiers='"linux-x64;linux-arm64"' \
    -p ContainerRuntimeIdentifiers='"linux-x64;linux-arm64"' \
    -p ContainerImageFormat=OCI
docker buildx imagetools inspect localhost:5000/dockerseries-bookstore-api:1.0.0-sdk
docker rm -f dockerseries-registry

# A console app needs no extra property in .NET 10
dotnet publish src/Bookstore.Tools/Bookstore.Tools.csproj -t:PublishContainer \
    -p ContainerRepository=dockerseries-bookstore-tools -p ContainerImageTags=1.0.0-sdk
docker run --rm dockerseries-bookstore-tools:1.0.0-sdk

# Compare with the Dockerfile image from post 8
docker build -f src/Bookstore.Api/Dockerfile -t dockerseries-bookstore-api:1.0.0 .
docker image ls --tree
docker history dockerseries-bookstore-api:1.0.0-sdk
docker rmi dockerseries-bookstore-tools:1.0.0-sdk
