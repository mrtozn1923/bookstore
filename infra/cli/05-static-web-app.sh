#!/usr/bin/env bash
# bookstore-web on Static Web Apps (Standard plan), with /api linked to the App Service API.
set -euo pipefail

: "${AZURE_SUBSCRIPTION_ID:?Set AZURE_SUBSCRIPTION_ID to the target subscription}"
ENVIRONMENT="${1:?Usage: 05-static-web-app.sh <dev|staging|prod>}"
RG="rg-bookstore-${ENVIRONMENT}-weu"
SWA="stapp-bookstore-${ENVIRONMENT}-weu"
API="app-bookstore-${ENVIRONMENT}-weu"

az account set --subscription "$AZURE_SUBSCRIPTION_ID"

az staticwebapp create --name "$SWA" --resource-group "$RG" --location westeurope --sku Standard

# Linked backend: requests to /api/* go to the API, same origin, no CORS
API_ID=$(az webapp show --name "$API" --resource-group "$RG" --query id --output tsv)
az staticwebapp backends link --name "$SWA" --resource-group "$RG" \
  --backend-resource-id "$API_ID" --backend-region westeurope

(cd web/bookstore-web && npm ci && npx ng build)
TOKEN=$(az staticwebapp secrets list --name "$SWA" --resource-group "$RG" --query properties.apiKey --output tsv)
npx @azure/static-web-apps-cli@2.1.0 deploy web/bookstore-web/dist/bookstore-web/browser \
  --deployment-token "$TOKEN" --env production
