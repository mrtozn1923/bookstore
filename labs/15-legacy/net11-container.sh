#!/usr/bin/env bash
# Docker series 15: .NET 11 SDK container publishing (local runtime selection, reproducible digests).
# Runs the SDK in a container with the engine's socket; run from the repository root.
set -uo pipefail
SDK11() {
    docker run --rm -v /var/run/docker.sock:/var/run/docker.sock -v /usr/local/bin/docker:/usr/local/bin/docker:ro -v "$PWD:/work" -w /work \
        -v dockerseries-nuget11:/root/.nuget/packages -e DOTNET_CLI_TELEMETRY_OPTOUT=1 -e DOTNET_NOLOGO=1 \
        mcr.microsoft.com/dotnet/sdk:11.0 sh -c "$1"
}
API=src/Bookstore.Api/Bookstore.Api.csproj

echo "### LocalRegistry pinned to a runtime that is not there"
SDK11 "dotnet publish $API -t:PublishContainer -p:LocalRegistry=Wslc -p ContainerImageTags=net11-wslc 2>&1 | grep -E 'error|warning|Pushed' | sed 's/ \[.*//'"

echo "### Reproducible digests with SOURCE_DATE_EPOCH"
EPOCH=$(git log -1 --pretty=%ct 2>/dev/null || echo 1794700800)
for i in 1 2; do
    SDK11 "dotnet publish $API -t:PublishContainer -p ContainerImageTags=repro-$i -p:SOURCE_DATE_EPOCH=$EPOCH > /dev/null"
done
for i in 3 4; do
    SDK11 "dotnet publish $API -t:PublishContainer -p ContainerImageTags=plain-$i > /dev/null"
done
docker image ls dockerseries-bookstore-api --format '{{.Tag}}  {{.ID}}' | grep -E "repro|plain" | sort
docker rmi dockerseries-bookstore-api:repro-1 dockerseries-bookstore-api:repro-2 \
    dockerseries-bookstore-api:plain-3 dockerseries-bookstore-api:plain-4
