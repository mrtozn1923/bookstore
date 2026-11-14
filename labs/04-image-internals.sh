#!/usr/bin/env bash
# Docker series 04: layers, copy-on-write, tags vs digests, a local registry and multi-platform indexes.
set -uo pipefail

docker pull nginx:1.30-alpine
docker image history --platform linux/amd64 nginx:1.30-alpine
docker image inspect --platform linux/amd64 nginx:1.30-alpine --format "Layers={{len .RootFS.Layers}} User={{.Config.User}} Entrypoint={{.Config.Entrypoint}} Cmd={{.Config.Cmd}} ExposedPorts={{.Config.ExposedPorts}} StopSignal={{.Config.StopSignal}}"

# Copy-on-write
docker run -d --name dockerseries-web nginx:1.30-alpine
sleep 2
docker exec dockerseries-web sh -c "echo \"<h1>Bookstore</h1>\" > /usr/share/nginx/html/index.html; rm /usr/share/nginx/html/50x.html"
docker diff dockerseries-web
docker ps -s --filter name=dockerseries-web --format "table {{.Names}}\t{{.Size}}"
docker rm -f dockerseries-web

# Shared layers
docker pull --platform linux/amd64 alpine:3

# Tag vs digest on a local registry
docker run -d --name dockerseries-registry -p 5000:5000 registry:3
sleep 2
docker tag nginx:1.30-alpine localhost:5000/bookstore/web:latest
docker push --platform linux/amd64 localhost:5000/bookstore/web:latest
docker tag alpine:3 localhost:5000/bookstore/web:latest
docker push --platform linux/amd64 localhost:5000/bookstore/web:latest
docker rmi localhost:5000/bookstore/web:latest
docker run --rm --platform linux/amd64 localhost:5000/bookstore/web:latest sh -c "cat /etc/alpine-release; command -v nginx || echo no nginx"
docker run --rm --platform linux/amd64 localhost:5000/bookstore/web@sha256:bb8f722b4be005fcd7827816a136ab779fd40185d85b5171be915cc7fc0575c7 nginx -v

# Registry HTTP API
curl -s localhost:5000/v2/_catalog
curl -s localhost:5000/v2/bookstore/web/tags/list
curl -s -D - -o /dev/null -H "Accept: application/vnd.oci.image.manifest.v1+json" -H "Accept: application/vnd.oci.image.index.v1+json" localhost:5000/v2/bookstore/web/manifests/latest | grep -i -E "content-type|docker-content-digest"

# Docker Hub pull limit (HEAD requests do not count)
TOKEN=$(curl -s "https://auth.docker.io/token?service=registry.docker.io&scope=repository:ratelimitpreview/test:pull" | jq -r .token)
curl -s --head -H "Authorization: Bearer $TOKEN" https://registry-1.docker.io/v2/ratelimitpreview/test/manifests/latest | grep -i '^ratelimit'

# Multi-platform index
docker buildx imagetools inspect nginx:1.30-alpine
docker buildx imagetools inspect --raw nginx:1.30-alpine@sha256:bb8f722b4be005fcd7827816a136ab779fd40185d85b5171be915cc7fc0575c7
docker buildx imagetools create --tag localhost:5000/bookstore/web:1.0.0 nginx:1.30-alpine
docker buildx imagetools inspect localhost:5000/bookstore/web:1.0.0 | head -3

# containerd image store
docker info -f "{{ .DriverStatus }}"
docker pull --platform linux/amd64 nginx:1.30-alpine
docker image ls --tree nginx:1.30-alpine

docker rm -f dockerseries-registry
