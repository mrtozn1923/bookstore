#!/usr/bin/env bash
# Creates the resource group of one Bookstore environment with the standard tags.
set -euo pipefail

: "${AZURE_SUBSCRIPTION_ID:?Set AZURE_SUBSCRIPTION_ID to the target subscription}"
ENVIRONMENT="${1:?Usage: 02-resource-group.sh <dev|staging|prod>}"
LOCATION="westeurope"
RG="rg-bookstore-${ENVIRONMENT}-weu"

az account set --subscription "$AZURE_SUBSCRIPTION_ID"

az group create \
  --name "$RG" \
  --location "$LOCATION" \
  --tags app=bookstore environment="$ENVIRONMENT" \
         owner=platform-team@example.com cost-center=cc-1042 \
  --output table
