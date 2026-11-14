#!/usr/bin/env bash
# Bookstore.Api on App Service: Linux plan, .NET 10 runtime, code deployment from a zip file.
set -euo pipefail

: "${AZURE_SUBSCRIPTION_ID:?Set AZURE_SUBSCRIPTION_ID to the target subscription}"
ENVIRONMENT="${1:?Usage: 05-app-service.sh <dev|staging|prod>}"
SKU="${SKU:-B1}"
RG="rg-bookstore-${ENVIRONMENT}-weu"
PLAN="asp-bookstore-${ENVIRONMENT}-weu"
APP="app-bookstore-${ENVIRONMENT}-weu"
IDENTITY="id-bookstore-${ENVIRONMENT}-weu"
STORAGE="stbookstore${ENVIRONMENT}weu"
SQL_SERVER="sql-bookstore-${ENVIRONMENT}-weu"

az account set --subscription "$AZURE_SUBSCRIPTION_ID"
IDENTITY_ID=$(az identity show --name "$IDENTITY" --resource-group "$RG" --query id --output tsv)
CLIENT_ID=$(az identity show --name "$IDENTITY" --resource-group "$RG" --query clientId --output tsv)

az appservice plan create --name "$PLAN" --resource-group "$RG" --is-linux --sku "$SKU"

az webapp create --name "$APP" --resource-group "$RG" --plan "$PLAN" \
  --runtime "DOTNETCORE:10.0" --assign-identity "$IDENTITY_ID" \
  --https-only true --basic-auth Disabled

az webapp config set --name "$APP" --resource-group "$RG" \
  --always-on true --min-tls-version 1.2 --ftps-state Disabled \
  --generic-configurations '{"healthCheckPath": "/health"}'

# App settings become environment variables; "__" is the section separator on Linux
az webapp config appsettings set --name "$APP" --resource-group "$RG" --settings \
  ASPNETCORE_ENVIRONMENT=Production \
  "Proxy__KnownNetworks=10.0.0.0/8;172.16.0.0/12;192.168.0.0/16" \
  "BlobStorage__ServiceUri=https://${STORAGE}.blob.core.windows.net" \
  "BlobStorage__ManagedIdentityClientId=${CLIENT_ID}"

# Passwordless: the identity signs in to Azure SQL (the database user is created in the managed databases post)
az webapp config connection-string set --name "$APP" --resource-group "$RG" \
  --connection-string-type SQLAzure --settings \
  "BookstoreDb=Server=tcp:${SQL_SERVER}.database.windows.net,1433;Database=BookstoreDb;Authentication=Active Directory Managed Identity;User Id=${CLIENT_ID};Encrypt=True"

dotnet publish src/Bookstore.Api --configuration Release --output publish
(cd publish && zip -qr ../bookstore-api.zip .)
az webapp deploy --name "$APP" --resource-group "$RG" --src-path bookstore-api.zip --type zip
