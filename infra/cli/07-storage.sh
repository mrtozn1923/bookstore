#!/usr/bin/env bash
# Blob Storage for covers: data protection, lifecycle rules and the role that lets Bookstore.Api sign user delegation SAS URLs.
set -euo pipefail

: "${AZURE_SUBSCRIPTION_ID:?Set AZURE_SUBSCRIPTION_ID to the target subscription}"
ENVIRONMENT="${1:?Usage: 07-storage.sh <dev|staging|prod>}"
RG="rg-bookstore-${ENVIRONMENT}-weu"
IDENTITY="id-bookstore-${ENVIRONMENT}-weu"
STORAGE="stbookstore${ENVIRONMENT}weu"

az account set --subscription "$AZURE_SUBSCRIPTION_ID"
PRINCIPAL_ID=$(az identity show --name "$IDENTITY" --resource-group "$RG" --query principalId --output tsv)
STORAGE_ID=$(az storage account show --name "$STORAGE" --resource-group "$RG" --query id --output tsv)

# A deleted or overwritten blob stays recoverable for 14 days; so does a deleted container
az storage account blob-service-properties update --account-name "$STORAGE" --resource-group "$RG" \
  --enable-delete-retention true --delete-retention-days 14 \
  --enable-container-delete-retention true --container-delete-retention-days 14 \
  --enable-last-access-tracking true

# Back office exports; covers came with 03-identity.sh, covers-thumbnails with 06-functions.sh
az storage container-rm create --storage-account "$STORAGE" --name exports

# Requests with a SAS that is valid for more than 3 hours, or has no start time, are rejected
az storage account update --name "$STORAGE" --resource-group "$RG" \
  --sas-expiration-period 0.03:00:00 --sas-expiration-action Block

# Lifecycle rules run on the platform side; the first run can take up to 24 hours after a change
az storage account management-policy create --account-name "$STORAGE" --resource-group "$RG" \
  --policy @infra/cli/07-lifecycle-policy.json

# generateUserDelegationKey is an account-level action: a container-scoped data role can't grant it
az role assignment create \
  --assignee-object-id "$PRINCIPAL_ID" --assignee-principal-type ServicePrincipal \
  --role "Storage Blob Delegator" --scope "$STORAGE_ID"
