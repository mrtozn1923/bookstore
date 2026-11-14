#!/usr/bin/env bash
# Runs once per subscription: tag rules for every Bookstore resource group and resource.
set -euo pipefail

: "${AZURE_SUBSCRIPTION_ID:?Set AZURE_SUBSCRIPTION_ID to the target subscription}"
SCOPE="/subscriptions/${AZURE_SUBSCRIPTION_ID}"

az account set --subscription "$AZURE_SUBSCRIPTION_ID"

# Built-in "Require a tag on resource groups": deny a resource group without cost-center
az policy assignment create \
  --name require-cost-center-on-rg \
  --display-name "Require cost-center tag on resource groups" \
  --policy 96670d01-0a4d-4649-9c89-2d3abc0a5025 \
  --params '{ "tagName": { "value": "cost-center" } }' \
  --scope "$SCOPE"

# Built-in "Inherit a tag from the resource group if missing": modify effect, needs an identity
az policy assignment create \
  --name inherit-cost-center \
  --display-name "Inherit cost-center tag from the resource group" \
  --policy ea3f2387-9b95-492a-a190-fcdc54f7b070 \
  --params '{ "tagName": { "value": "cost-center" } }' \
  --scope "$SCOPE" \
  --mi-system-assigned --location westeurope \
  --identity-scope "$SCOPE" --role Contributor
