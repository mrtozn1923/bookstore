#!/usr/bin/env bash
# Smoke test after a deployment: GET /api/categories must return 200 with a non-empty JSON array.
# A freshly deployed App Service can need a few attempts while it starts.
set -euo pipefail

base_url="${1:?usage: smoke-test.sh <base-url>}"
attempts="${SMOKE_ATTEMPTS:-10}"
delay="${SMOKE_DELAY_SECONDS:-15}"
url="${base_url%/}/api/categories"
body=$(mktemp)
trap 'rm -f "$body"' EXIT

for attempt in $(seq 1 "$attempts"); do
  status=$(curl --silent --output "$body" --write-out '%{http_code}' --max-time 10 "$url" || true)
  if [ "$status" = "200" ] && [ "$(jq 'length' "$body" 2>/dev/null || echo 0)" -gt 0 ]; then
    echo "Smoke test passed on attempt $attempt: $url returned $(jq 'length' "$body") categories"
    exit 0
  fi
  echo "Attempt $attempt/$attempts: $url returned HTTP $status"
  if [ "$attempt" -lt "$attempts" ]; then
    sleep "$delay"
  fi
done

echo "##vso[task.logissue type=error]Smoke test failed: $url did not return categories after $attempts attempts"
exit 1
