#!/usr/bin/env bash
# Bookstore.Functions on Flex Consumption: cover thumbnails, identity-based storage access, Event Grid subscription.
set -euo pipefail

: "${AZURE_SUBSCRIPTION_ID:?Set AZURE_SUBSCRIPTION_ID to the target subscription}"
ENVIRONMENT="${1:?Usage: 06-functions.sh <dev|staging|prod>}"
RG="rg-bookstore-${ENVIRONMENT}-weu"
FUNC="func-bookstore-${ENVIRONMENT}-weu"
IDENTITY="id-bookstore-${ENVIRONMENT}-weu"
STORAGE="stbookstore${ENVIRONMENT}weu"
HOST_STORAGE="stbookstorefn${ENVIRONMENT}weu"

az account set --subscription "$AZURE_SUBSCRIPTION_ID"
IDENTITY_ID=$(az identity show --name "$IDENTITY" --resource-group "$RG" --query id --output tsv)
CLIENT_ID=$(az identity show --name "$IDENTITY" --resource-group "$RG" --query clientId --output tsv)
PRINCIPAL_ID=$(az identity show --name "$IDENTITY" --resource-group "$RG" --query principalId --output tsv)
STORAGE_ID=$(az storage account show --name "$STORAGE" --resource-group "$RG" --query id --output tsv)

# The Functions host keeps its own state (deployment package, triggers' queues) in a separate account
az storage account create --name "$HOST_STORAGE" --resource-group "$RG" --location westeurope \
  --sku Standard_LRS --allow-blob-public-access false

az functionapp create --name "$FUNC" --resource-group "$RG" \
  --storage-account "$HOST_STORAGE" --flexconsumption-location westeurope \
  --runtime dotnet-isolated --runtime-version 10.0 \
  --instance-memory 2048 --maximum-instance-count 20 \
  --assign-identity "$IDENTITY_ID"

# The trigger reads covers and writes covers-thumbnails; the blob trigger also needs queues on that account
az storage container-rm create --storage-account "$STORAGE" --name covers-thumbnails
for ROLE in "Storage Blob Data Owner" "Storage Queue Data Contributor"; do
  az role assignment create --assignee-object-id "$PRINCIPAL_ID" --assignee-principal-type ServicePrincipal \
    --role "$ROLE" --scope "$STORAGE_ID"
done

# Identity-based connection: no connection string, only the account's service URIs and the identity
az functionapp config appsettings set --name "$FUNC" --resource-group "$RG" --settings \
  "CoversStorage__blobServiceUri=https://${STORAGE}.blob.core.windows.net" \
  "CoversStorage__queueServiceUri=https://${STORAGE}.queue.core.windows.net" \
  "CoversStorage__credential=managedidentity" \
  "CoversStorage__clientId=${CLIENT_ID}"

(cd src/Bookstore.Functions && func azure functionapp publish "$FUNC")

# Event Grid calls the blob extension's webhook; the key protects it
KEY=$(az functionapp keys list --name "$FUNC" --resource-group "$RG" --query "systemKeys.blobs_extension" --output tsv)
ENDPOINT="https://${FUNC}.azurewebsites.net/runtime/webhooks/blobs?functionName=Host.Functions.CoverThumbnail&code=${KEY}"
az eventgrid event-subscription create --name covers-created \
  --source-resource-id "$STORAGE_ID" --endpoint "$ENDPOINT" \
  --included-event-types Microsoft.Storage.BlobCreated \
  --subject-begins-with /blobServices/default/containers/covers/blobs/
