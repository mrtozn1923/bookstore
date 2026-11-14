#!/usr/bin/env bash
# Bookstore.Api on Container Apps: the same API as a container image from GitHub Container Registry.
set -euo pipefail

: "${AZURE_SUBSCRIPTION_ID:?Set AZURE_SUBSCRIPTION_ID to the target subscription}"
ENVIRONMENT="${1:?Usage: 05-container-apps.sh <dev|staging|prod> <image>}"
IMAGE="${2:?Usage: 05-container-apps.sh <dev|staging|prod> ghcr.io/<owner>/bookstore-api:<tag>}"
RG="rg-bookstore-${ENVIRONMENT}-weu"
CAE="cae-bookstore-${ENVIRONMENT}-weu"
APP="ca-bookstore-${ENVIRONMENT}-weu"
IDENTITY="id-bookstore-${ENVIRONMENT}-weu"
STORAGE="stbookstore${ENVIRONMENT}weu"
SQL_SERVER="sql-bookstore-${ENVIRONMENT}-weu"

az account set --subscription "$AZURE_SUBSCRIPTION_ID"
IDENTITY_ID=$(az identity show --name "$IDENTITY" --resource-group "$RG" --query id --output tsv)
CLIENT_ID=$(az identity show --name "$IDENTITY" --resource-group "$RG" --query clientId --output tsv)

az containerapp env create --name "$CAE" --resource-group "$RG" --location westeurope

az containerapp create --name "$APP" --resource-group "$RG" --environment "$CAE" \
  --image "$IMAGE" --target-port 8080 --ingress external \
  --cpu 0.5 --memory 1.0Gi --min-replicas 0 --max-replicas 5 \
  --scale-rule-name http --scale-rule-type http --scale-rule-http-concurrency 50 \
  --user-assigned "$IDENTITY_ID" --revisions-mode multiple \
  --env-vars ASPNETCORE_ENVIRONMENT=Production \
    "Proxy__KnownNetworks=10.0.0.0/8;172.16.0.0/12;192.168.0.0/16" \
    "BlobStorage__ServiceUri=https://${STORAGE}.blob.core.windows.net" \
    "BlobStorage__ManagedIdentityClientId=${CLIENT_ID}" \
    "ConnectionStrings__BookstoreDb=Server=tcp:${SQL_SERVER}.database.windows.net,1433;Database=BookstoreDb;Authentication=Active Directory Managed Identity;User Id=${CLIENT_ID};Encrypt=True"

az containerapp show --name "$APP" --resource-group "$RG" --query properties.configuration.ingress.fqdn --output tsv
