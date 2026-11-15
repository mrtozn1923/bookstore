#!/usr/bin/env bash
# Fails (non-zero exit) when a package's license is not in eng/allowed-licenses.json
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet tool restore
dotnet nuget-license -i Bookstore.slnx -t \
    -a eng/allowed-licenses.json \
    -override eng/license-overrides.json
