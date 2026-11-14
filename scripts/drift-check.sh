#!/usr/bin/env bash
# Fails when a plan would change a resource: drift or code that was never applied
set -euo pipefail

terraform plan -input=false -lock=false -no-color -out=drift.tfplan > drift.txt
cat drift.txt

# Data sources read during apply (our check block) are not changes
changes=$(terraform show -json drift.tfplan |
    jq '[.resource_changes[]? | select(.change.actions != ["no-op"] and .change.actions != ["read"])] | length')

if grep -q "Check block assertion failed" drift.txt; then
    echo "::warning::A check block failed; see the plan output."
fi

if [ "$changes" -gt 0 ]; then
    echo "::error::Drift detected: $changes resource(s) would change."
    exit 1
fi
echo "No drift."
