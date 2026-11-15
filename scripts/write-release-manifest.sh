#!/usr/bin/env bash
# Writes manifest.json into the release folder so a deployment knows exactly what it carries.
set -euo pipefail

release_dir="${1:?usage: write-release-manifest.sh <release-dir>}"
version="${APP_VERSION:?APP_VERSION is required}"
commit="${BUILD_SOURCEVERSION:-unknown}"
run="${BUILD_BUILDNUMBER:-local}"

api_files=$(find "$release_dir/api" -type f | wc -l | tr -d ' ')
web_files=$(find "$release_dir/web" -type f | wc -l | tr -d ' ')

cat > "$release_dir/manifest.json" <<JSON
{
  "version": "${version}",
  "commit": "${commit}",
  "run": "${run}",
  "createdUtc": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "api": { "files": ${api_files} },
  "web": { "files": ${web_files} }
}
JSON

cat "$release_dir/manifest.json"
