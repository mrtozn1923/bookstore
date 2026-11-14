#!/usr/bin/env bash
# Docker series 06: build cache, cache mounts, build secrets, multi-platform and Bake. Run from the repository root.
set -uo pipefail

# Layer order + cache mount: change a .cs file and rebuild, restore stays CACHED
docker build --check -f src/Bookstore.Tools/Dockerfile .
docker build --progress=plain -f src/Bookstore.Tools/Dockerfile -t dockerseries-bookstore-tools:1.0.0 .
docker build --no-cache --progress=plain -f src/Bookstore.Tools/Dockerfile -t dockerseries-bookstore-tools:1.0.0 .
docker buildx du --filter type=exec.cachemount --verbose

# Build secret: a private NuGet feed that wants a token
export NUGET_TOKEN=$(openssl rand -hex 16)
labs/06-private-feed/publish-package.sh
docker run -d --name dockerseries-feed -p 8085:80 \
    -e FEED_BASIC_AUTH="$(printf 'bookstore-ci:%s' "$NUGET_TOKEN" | base64)" \
    -v "$PWD/labs/06-private-feed/feed:/usr/share/nginx/feed:ro" \
    -v "$PWD/labs/06-private-feed/feed.conf.template:/etc/nginx/templates/default.conf.template:ro" \
    nginx:1.30-alpine
sleep 2
curl -s -o /dev/null -w "%{http_code}\n" localhost:8085/v3/index.json
docker build --add-host feed.bookstore.test=host-gateway -t dockerseries-isbncheck labs/06-private-feed
docker build --add-host feed.bookstore.test=host-gateway --secret id=nuget_token,env=NUGET_TOKEN \
    -t dockerseries-isbncheck labs/06-private-feed
docker run --rm dockerseries-isbncheck 9780441172719 9780141439510
docker history --no-trunc --format "{{.CreatedBy}}" dockerseries-isbncheck | head -5
docker rm -f dockerseries-feed

# Builders and a multi-platform image on a local registry
docker run -d --name dockerseries-registry -p 5000:5000 registry:3
docker buildx ls
docker buildx build --platform linux/amd64,linux/arm64 \
    -t localhost:5000/bookstore/platform-demo:1.0.0 --push labs/06-multi-platform
docker buildx imagetools inspect localhost:5000/bookstore/platform-demo:1.0.0
docker run --rm --platform linux/arm64 localhost:5000/bookstore/platform-demo:1.0.0

# Bake
docker buildx bake --print tools
docker buildx bake ci
docker buildx bake tools-release
docker rm -f dockerseries-registry
docker rmi dockerseries-isbncheck
