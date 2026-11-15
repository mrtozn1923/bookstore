#!/usr/bin/env bash
# Lists every resource a saved plan changes, with the attributes that force a replacement.
# Usage: scripts/plan-changes.sh tfplan   (run inside the root module that made the plan)
set -euo pipefail

plan=${1:-tfplan}

# The JSON plan holds every value in clear text, sensitive ones too: never save or upload it
terraform show -json "$plan" | jq -r '
  .resource_changes[]?
  | select(.change.actions != ["no-op"] and .change.actions != ["read"])
  | [
      (.change.actions | join("+")),
      .address,
      (.change.replace_paths // [] | map(map(tostring) | join(".")) | join(", "))
    ]
  | @tsv'
