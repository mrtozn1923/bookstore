#!/usr/bin/env bash
# Compressed (download) size of one platform of an image, read from the registry; nothing is pulled.
# Usage: image-sizes.sh <image> <os/arch>   e.g. image-sizes.sh mcr.microsoft.com/windows/nanoserver:ltsc2025 windows/amd64
set -euo pipefail
image=$1 platform=$2
repo=${image%:*}
raw=$(docker buildx imagetools inspect --raw "$image")
if [ "$(jq -r '.manifests // empty | length' <<<"$raw")" != "" ]; then
    digest=$(jq -r --arg os "${platform%/*}" --arg arch "${platform#*/}" \
        'first(.manifests[] | select(.platform.os == $os and .platform.architecture == $arch)) | .digest' <<<"$raw")
    raw=$(docker buildx imagetools inspect --raw "$repo@$digest")
fi
jq -r --arg image "$image" '"\($image)  layers=\(.layers | length)  compressed=\((.layers | map(.size) | add) / 1000000 | floor) MB"' <<<"$raw"
