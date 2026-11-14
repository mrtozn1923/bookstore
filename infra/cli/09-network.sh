#!/usr/bin/env bash
# Bookstore's network: a VNet, delegated subnets, private endpoints for SQL and Blob, then public access off.
set -euo pipefail

: "${AZURE_SUBSCRIPTION_ID:?Set AZURE_SUBSCRIPTION_ID to the target subscription}"
ENVIRONMENT="${1:?Usage: 09-network.sh <dev|staging|prod>}"
LOCATION="westeurope"
RG="rg-bookstore-${ENVIRONMENT}-weu"
VNET="vnet-bookstore-${ENVIRONMENT}-weu"
NSG_PE="nsg-bookstore-pe-${ENVIRONMENT}-weu"
APP="app-bookstore-${ENVIRONMENT}-weu"
SQL_SERVER="sql-bookstore-${ENVIRONMENT}-weu"
STORAGE="stbookstore${ENVIRONMENT}weu"

az account set --subscription "$AZURE_SUBSCRIPTION_ID"

# Address plan from tools/cloud/subnets.cs; no default outbound internet access in any subnet
az network vnet create --name "$VNET" --resource-group "$RG" --location "$LOCATION" \
  --address-prefixes 10.20.0.0/16
az network vnet subnet create --vnet-name "$VNET" --resource-group "$RG" --name snet-app \
  --address-prefixes 10.20.1.0/26 --delegations Microsoft.Web/serverFarms --default-outbound false

# Private endpoint subnet: only the app subnet may reach SQL (1433) and Blob (443)
az network nsg create --name "$NSG_PE" --resource-group "$RG" --location "$LOCATION"
az network nsg rule create --nsg-name "$NSG_PE" --resource-group "$RG" --name allow-app-to-data \
  --priority 100 --direction Inbound --access Allow --protocol Tcp \
  --source-address-prefixes 10.20.1.0/26 --destination-port-ranges 1433 443
az network nsg rule create --nsg-name "$NSG_PE" --resource-group "$RG" --name deny-other-vnet \
  --priority 4000 --direction Inbound --access Deny --protocol '*' \
  --source-address-prefixes VirtualNetwork --destination-port-ranges '*'
az network vnet subnet create --vnet-name "$VNET" --resource-group "$RG" --name snet-pe \
  --address-prefixes 10.20.3.0/27 --nsg "$NSG_PE" --default-outbound false \
  --private-endpoint-network-policies NetworkSecurityGroupEnabled

# Private DNS: inside the VNet, these zones answer for the privatelink.* names
for ZONE in privatelink.database.windows.net privatelink.blob.core.windows.net; do
  az network private-dns zone create --resource-group "$RG" --name "$ZONE"
  az network private-dns link vnet create --resource-group "$RG" --zone-name "$ZONE" \
    --name "link-${VNET}" --virtual-network "$VNET" --registration-enabled false
done

SQL_ID=$(az sql server show --name "$SQL_SERVER" --resource-group "$RG" --query id --output tsv)
STORAGE_ID=$(az storage account show --name "$STORAGE" --resource-group "$RG" --query id --output tsv)

# One private endpoint per resource and sub-resource; the zone group keeps the A record up to date
az network private-endpoint create --name "pe-sql-bookstore-${ENVIRONMENT}-weu" --resource-group "$RG" \
  --vnet-name "$VNET" --subnet snet-pe --private-connection-resource-id "$SQL_ID" \
  --group-id sqlServer --connection-name sql
az network private-endpoint dns-zone-group create --endpoint-name "pe-sql-bookstore-${ENVIRONMENT}-weu" \
  --resource-group "$RG" --name default --private-dns-zone privatelink.database.windows.net --zone-name sql

az network private-endpoint create --name "pe-blob-bookstore-${ENVIRONMENT}-weu" --resource-group "$RG" \
  --vnet-name "$VNET" --subnet snet-pe --private-connection-resource-id "$STORAGE_ID" \
  --group-id blob --connection-name blob
az network private-endpoint dns-zone-group create --endpoint-name "pe-blob-bookstore-${ENVIRONMENT}-weu" \
  --resource-group "$RG" --name default --private-dns-zone privatelink.blob.core.windows.net --zone-name blob

# Bookstore.Api's outbound traffic enters the VNet, so its DNS lookups see the private zones
az webapp vnet-integration add --name "$APP" --resource-group "$RG" --vnet "$VNET" --subnet snet-app
az webapp config set --name "$APP" --resource-group "$RG" --vnet-route-all-enabled true

# Close the public doors last: doing it earlier would cut the API off
az sql server update --name "$SQL_SERVER" --resource-group "$RG" --enable-public-network false

# Browsers download covers with SAS URLs (post 07) and the function app reads covers too. Close Blob
# only after covers go through Front Door Premium with a Private Link origin and the function is in the VNet.
if [[ "${CLOSE_STORAGE:-false}" == "true" ]]; then
  az storage account update --name "$STORAGE" --resource-group "$RG" --public-network-access Disabled
fi
