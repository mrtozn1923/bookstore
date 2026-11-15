#!/usr/bin/env bash
# Usage: scripts/check-contract.sh <base-contract.json>
set -euo pipefail
base="$1"

# 1. Build regenerates contracts/Bookstore.Api.json; it must already be committed
dotnet build src/Bookstore.Api -c Release --nologo -v quiet
git diff --exit-code -- contracts/Bookstore.Api.json

# 2. Compared with the base branch: no breaking change allowed
oasdiff breaking "$base" contracts/Bookstore.Api.json --fail-on ERR

# 3. Angular types are regenerated from the contract and the code must still compile
(cd web && npm run --silent api:types && npm run --silent typecheck)
echo "contract OK"
