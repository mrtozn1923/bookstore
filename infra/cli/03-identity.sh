#!/usr/bin/env bash
# Bookstore.Api identity: a user-assigned managed identity that may only read and write the covers container.
set -euo pipefail

: "${AZURE_SUBSCRIPTION_ID:?Set AZURE_SUBSCRIPTION_ID to the target subscription}"
ENVIRONMENT="${1:?Usage: 03-identity.sh <dev|staging|prod>}"
LOCATION="westeurope"
RG="rg-bookstore-${ENVIRONMENT}-weu"
IDENTITY="id-bookstore-${ENVIRONMENT}-weu"
STORAGE="stbookstore${ENVIRONMENT}weu"

az account set --subscription "$AZURE_SUBSCRIPTION_ID"

# Storage account that accepts only Microsoft Entra ID: no account key, no anonymous access
az storage account create \
  --name "$STORAGE" --resource-group "$RG" --location "$LOCATION" \
  --sku Standard_LRS --kind StorageV2 --min-tls-version TLS1_2 \
  --allow-shared-key-access false --allow-blob-public-access false

# Control plane: creating the container goes through ARM, no data role needed
az storage container-rm create --storage-account "$STORAGE" --name covers

az identity create --name "$IDENTITY" --resource-group "$RG" --location "$LOCATION"
PRINCIPAL_ID=$(az identity show --name "$IDENTITY" --resource-group "$RG" --query principalId --output tsv)
CLIENT_ID=$(az identity show --name "$IDENTITY" --resource-group "$RG" --query clientId --output tsv)
STORAGE_ID=$(az storage account show --name "$STORAGE" --resource-group "$RG" --query id --output tsv)

# Data plane: the narrowest scope is the container, not the account or the resource group
az role assignment create \
  --assignee-object-id "$PRINCIPAL_ID" --assignee-principal-type ServicePrincipal \
  --role "Storage Blob Data Contributor" \
  --scope "${STORAGE_ID}/blobServices/default/containers/covers"

echo "BlobStorage__ServiceUri=https://${STORAGE}.blob.core.windows.net"
echo "BlobStorage__ManagedIdentityClientId=${CLIENT_ID}"
