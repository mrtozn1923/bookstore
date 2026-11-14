#!/usr/bin/env bash
# Packs Bookstore.Isbn and lays it out as a static NuGet v3 feed (flat container) under feed/v3.
set -euo pipefail
cd "$(dirname "$0")"
docker run --rm -v "$PWD:/lab" -w /lab mcr.microsoft.com/dotnet/sdk:10.0 \
    dotnet pack Bookstore.Isbn/Bookstore.Isbn.csproj -c Release -o /lab/out
mkdir -p feed/v3/flatcontainer/bookstore.isbn/1.0.0
cp out/Bookstore.Isbn.1.0.0.nupkg feed/v3/flatcontainer/bookstore.isbn/1.0.0/bookstore.isbn.1.0.0.nupkg
echo '{ "versions": [ "1.0.0" ] }' > feed/v3/flatcontainer/bookstore.isbn/index.json
rm -rf out Bookstore.Isbn/bin Bookstore.Isbn/obj
