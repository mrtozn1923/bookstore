#!/usr/bin/env bash
# Docker series 07: multi-stage build with restore, build, test, export, debug and final stages. Run from the repository root.
set -uo pipefail

docker build --check -f src/Bookstore.Tools/Dockerfile .

# Default target = last stage (final); the test stage is skipped
docker build --progress=plain -f src/Bookstore.Tools/Dockerfile -t dockerseries-bookstore-tools:1.0.0 .
docker run --rm dockerseries-bookstore-tools:1.0.0

# Test stage on demand
docker build --progress=plain --target test -f src/Bookstore.Tools/Dockerfile .

# Publish output to the host through a FROM scratch stage
docker build --target export --output type=local,dest=out -f src/Bookstore.Tools/Dockerfile .
ls out && rm -rf out

# No shell in the chiseled image; the debug target brings busybox
docker run --rm --entrypoint sh dockerseries-bookstore-tools:1.0.0
docker build --target debug -f src/Bookstore.Tools/Dockerfile -t dockerseries-bookstore-tools:1.0.0-debug .
docker run --rm --entrypoint /busybox/busybox dockerseries-bookstore-tools:1.0.0-debug ls -ln /app/data

# Sizes
docker image ls --tree

# Bake: lint, test and image in one session; multi-architecture release without QEMU
docker buildx bake ci
docker run -d --name dockerseries-registry -p 5000:5000 registry:3
docker buildx bake tools-release
docker buildx imagetools inspect localhost:5000/bookstore/tools:1.0.0
docker run --rm --platform linux/arm64 localhost:5000/bookstore/tools:1.0.0
docker rm -f dockerseries-registry
docker rmi dockerseries-bookstore-tools:1.0.0-debug
