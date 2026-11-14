#!/usr/bin/env bash
# Writes Cloudflare's published edge ranges to src/Bookstore.Api/cloudflare-ips.txt.
# Run it in CI (daily) and on deploy; fail instead of writing an empty list.
set -euo pipefail
out=src/Bookstore.Api/cloudflare-ips.txt
json=$(curl -sf https://api.cloudflare.com/client/v4/ips)
jq -e '.success' <<<"$json" >/dev/null
jq -r '.result.ipv4_cidrs[], .result.ipv6_cidrs[]' <<<"$json" > "$out.new"
[[ $(wc -l < "$out.new") -gt 10 ]] || { echo "suspiciously short list" >&2; exit 1; }
mv "$out.new" "$out"
echo "etag $(jq -r '.result.etag' <<<"$json"), $(wc -l < "$out" | tr -d ' ') ranges"
