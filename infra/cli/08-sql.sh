#!/usr/bin/env bash
# Azure SQL Database for BookstoreDb: Microsoft Entra-only authentication, serverless compute, backup retention.
set -euo pipefail

: "${AZURE_SUBSCRIPTION_ID:?Set AZURE_SUBSCRIPTION_ID to the target subscription}"
ENVIRONMENT="${1:?Usage: 08-sql.sh <dev|staging|prod>}"
LOCATION="westeurope"
RG="rg-bookstore-${ENVIRONMENT}-weu"
SQL_SERVER="sql-bookstore-${ENVIRONMENT}-weu"
DBA_GROUP="sg-bookstore-dba"

az account set --subscription "$AZURE_SUBSCRIPTION_ID"
DBA_GROUP_ID=$(az ad group show --group "$DBA_GROUP" --query id --output tsv)

# No SQL logins at all: only Entra identities sign in, and the server admin is a group, not a person
az sql server create --name "$SQL_SERVER" --resource-group "$RG" --location "$LOCATION" \
  --enable-ad-only-auth --external-admin-principal-type Group \
  --external-admin-name "$DBA_GROUP" --external-admin-sid "$DBA_GROUP_ID" \
  --minimal-tls-version 1.2

if [[ "$ENVIRONMENT" == "dev" ]]; then
  # Free offer: when the monthly allowance is used up, the database pauses instead of billing
  az sql db create --name BookstoreDb --server "$SQL_SERVER" --resource-group "$RG" \
    --edition GeneralPurpose --family Gen5 --capacity 2 --compute-model Serverless \
    --use-free-limit --free-limit-exhaustion-behavior AutoPause \
    --backup-storage-redundancy Local
else
  # Serverless between 0.5 and 4 vCores, never paused, replicas spread over availability zones
  az sql db create --name BookstoreDb --server "$SQL_SERVER" --resource-group "$RG" \
    --edition GeneralPurpose --family Gen5 --capacity 4 --min-capacity 0.5 --compute-model Serverless \
    --auto-pause-delay -1 --zone-redundant true --backup-storage-redundancy Zone
fi

# Point-in-time restore window: 14 days instead of the default 7
az sql db str-policy set --name BookstoreDb --server "$SQL_SERVER" --resource-group "$RG" \
  --retention-days 14 --diffbackup-hours 12

if [[ "$ENVIRONMENT" == "prod" ]]; then
  # Long-term retention: weekly backups for 8 weeks, monthly backups for 12 months
  az sql db ltr-policy set --name BookstoreDb --server "$SQL_SERVER" --resource-group "$RG" \
    --weekly-retention P8W --monthly-retention P12M
fi
