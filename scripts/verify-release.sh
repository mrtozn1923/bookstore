#!/usr/bin/env bash
# Checks that the downloaded release artifact is complete and was built from the expected commit
# before anything is deployed. Reads manifest.json written by write-release-manifest.sh.
set -euo pipefail

release_dir="${1:?usage: verify-release.sh <release-dir>}"
manifest="$release_dir/manifest.json"

fail() {
  echo "##vso[task.logissue type=error]$1"
  exit 1
}

[ -f "$manifest" ] || fail "manifest.json not found in $release_dir"

version=$(jq -r '.version' "$manifest")
commit=$(jq -r '.commit' "$manifest")
api_expected=$(jq -r '.api.files' "$manifest")
web_expected=$(jq -r '.web.files' "$manifest")
api_actual=$(find "$release_dir/api" -type f | wc -l | tr -d ' ')
web_actual=$(find "$release_dir/web" -type f | wc -l | tr -d ' ')

[ -f "$release_dir/api/Bookstore.Api.dll" ] || fail "api/Bookstore.Api.dll is missing"
[ -f "$release_dir/web/index.html" ] || fail "web/index.html is missing"
[ "$api_actual" = "$api_expected" ] || fail "api has $api_actual files, manifest says $api_expected"
[ "$web_actual" = "$web_expected" ] || fail "web has $web_actual files, manifest says $web_expected"
if [ -n "${EXPECTED_COMMIT:-}" ] && [ "$commit" != "$EXPECTED_COMMIT" ]; then
  fail "release was built from $commit, expected $EXPECTED_COMMIT"
fi

echo "Release $version verified: commit $commit, api $api_actual files, web $web_actual files"
