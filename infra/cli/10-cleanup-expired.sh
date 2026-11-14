#!/usr/bin/env bash
# Finds resource groups whose expires-on tag (yyyy-mm-dd) is in the past; deletes them only with --delete.
set -euo pipefail

: "${AZURE_SUBSCRIPTION_ID:?Set AZURE_SUBSCRIPTION_ID to the target subscription}"
TODAY=$(date -u +%Y-%m-%d)

az account set --subscription "$AZURE_SUBSCRIPTION_ID"

# ISO dates sort as strings, so a string comparison is a date comparison
EXPIRED=$(az group list --output tsv \
  --query "[?tags.\"expires-on\" && tags.\"expires-on\" < '${TODAY}'].name")

for RG in $EXPIRED; do
  if [[ "${1:-}" == "--delete" ]]; then
    az group delete --name "$RG" --yes --no-wait
    echo "deleting ${RG}"
  else
    echo "would delete ${RG} (run with --delete)"
  fi
done
