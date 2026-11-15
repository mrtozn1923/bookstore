#!/usr/bin/env bash
# Docker series 16: lesser-known Dockerfile instructions and flags. Run from the repository root.
# Variants of the lab Dockerfiles are generated into out/16 (out/ is in .dockerignore).
set -uo pipefail
L=labs/16-dockerfile
mkdir -p out/16

echo "### Which version does docker/dockerfile:1 point to?"
for t in 1 1.27.1 1.28.0 1.29.0; do
    printf "dockerfile:%-7s " $t
    docker buildx imagetools inspect docker/dockerfile:$t --format '{{.Manifest.Digest}}' | cut -c1-19
done
docker build --progress=plain --no-cache -f $L/Dockerfile.stray-flag .
sed 's#docker/dockerfile:1$#docker/dockerfile:1.29.0#' $L/Dockerfile.stray-flag > out/16/Dockerfile.stray-flag-1.29
docker build --progress=plain --no-cache -f out/16/Dockerfile.stray-flag-1.29 .

echo "### Heredocs"
docker build --progress=plain --no-cache -f $L/Dockerfile.heredoc .
awk 'NR==4{print "set -eu"} {print}' $L/Dockerfile.heredoc > out/16/Dockerfile.heredoc-set-e
docker build --progress=plain --no-cache -f out/16/Dockerfile.heredoc-set-e .
docker build --progress=plain --no-cache -f $L/Dockerfile.heredoc-unquoted .
docker build --check -f $L/Dockerfile.heredoc-unquoted .

echo "### ADD from a URL and from Git, with checksums"
docker build --progress=plain -f $L/Dockerfile.dbinit -t dockerseries-bookstore-dbinit:1.3.0 .
docker run --rm --entrypoint sqlcmd dockerseries-bookstore-dbinit:1.3.0 --version
docker run --rm dockerseries-bookstore-dbinit:1.3.0
sed 's/92516d98c63d/92516d98c63e/; s/9faaa981f9c3/9faaa981f9c4/' $L/Dockerfile.dbinit > out/16/Dockerfile.dbinit-bad
docker build --progress=plain -f out/16/Dockerfile.dbinit-bad .
docker build --progress=plain --no-cache -f $L/Dockerfile.add-git .
sed 's/affbc17e1e0e/affbc17e1e0f/' $L/Dockerfile.add-git > out/16/Dockerfile.add-git-bad
docker build --progress=plain --no-cache -f out/16/Dockerfile.add-git-bad .

echo "### COPY --parents, --link with --chown"
docker build --progress=plain --target flat -f $L/Dockerfile.parents .
docker build --progress=plain --target parents -f $L/Dockerfile.parents .
docker build --progress=plain --no-cache -f $L/Dockerfile.link-chown .
sed 's/1.29.0/1.27.1/' $L/Dockerfile.link-chown > out/16/Dockerfile.link-chown-1.27
docker build --progress=plain --no-cache -f out/16/Dockerfile.link-chown-1.27 .

echo "### RUN --network=none"
docker build --progress=plain --no-cache -f $L/Dockerfile.network-none .

echo "### SHELL and pipefail"
docker build --progress=plain --no-cache -f $L/Dockerfile.pipefail .
awk 'NR==3{print "SHELL [\"/bin/bash\", \"-o\", \"pipefail\", \"-c\"]"} {print}' $L/Dockerfile.pipefail > out/16/Dockerfile.pipefail-fixed
docker build --progress=plain --no-cache -f out/16/Dockerfile.pipefail-fixed .

echo "### ONBUILD and BUILDKIT_DISABLE_ONBUILD"
docker build -f $L/Dockerfile.onbuild-base -t dockerseries-onbuild-base:1.0 .
docker image inspect dockerseries-onbuild-base:1.0 --format '{{json .Config.OnBuild}}'
docker build --progress=plain --no-cache -f $L/Dockerfile.onbuild-child .
docker build --progress=plain --no-cache --build-arg BUILDKIT_DISABLE_ONBUILD=1 -f $L/Dockerfile.onbuild-child .

echo "### Build checks and hadolint"
docker build --check -f src/Bookstore.Api/Dockerfile .
sed 's/ AS build$/ as build/' src/Bookstore.Api/Dockerfile > out/16/Dockerfile.api-casing
docker build -f out/16/Dockerfile.api-casing .
for f in $L/Dockerfile.pipefail labs/15-legacy/Dockerfile.2021 src/Bookstore.Api/Dockerfile $L/Dockerfile.dbinit; do
    echo "== $f"
    docker run --rm -i -v "$PWD/.hadolint.yaml:/.config/hadolint.yaml:ro" hadolint/hadolint:v2.15.1 hadolint --no-color - < "$f"
done

echo "### Bookstore.Api 1.3.0"
docker buildx bake lint
GIT_SHA=$(git rev-parse --short HEAD 2>/dev/null || echo unknown) docker buildx bake api
docker image inspect dockerseries-bookstore-api:1.3.0 --format '{{json .Config.Labels}}'

docker rmi dockerseries-bookstore-dbinit:1.3.0 dockerseries-onbuild-base:1.0
rm -rf out/16
