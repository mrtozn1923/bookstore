#!/usr/bin/env bash
# Restore drill on Azure SQL: point-in-time restore of BookstoreDb into a new database, timed, then deleted.
# Uses GNU date (Cloud Shell, WSL, Git Bash, GitHub-hosted runners).
set -euo pipefail

: "${AZURE_SUBSCRIPTION_ID:?Set AZURE_SUBSCRIPTION_ID to the target subscription}"
ENVIRONMENT="${1:?Usage: 11-restore-drill.sh <dev|staging|prod>}"
RG="rg-bookstore-${ENVIRONMENT}-weu"
SQL_SERVER="sql-bookstore-${ENVIRONMENT}-weu"
DRILL="BookstoreDb-drill-$(date -u +%Y%m%d)"
POINT_IN_TIME=$(date -u -d '1 hour ago' +%Y-%m-%dT%H:%M:%SZ)

az account set --subscription "$AZURE_SUBSCRIPTION_ID"

# Restore always creates a new database; the original is never touched
STARTED=$(date +%s)
az sql db restore --resource-group "$RG" --server "$SQL_SERVER" --name BookstoreDb \
  --dest-name "$DRILL" --time "$POINT_IN_TIME"
echo "Restored ${POINT_IN_TIME} into ${DRILL} in $(( $(date +%s) - STARTED )) s"

# Compare the copy with the source (row counts, last order) before deleting it, e.g. with tools/cloud/restore-drill.cs queries
read -r -p "Delete ${DRILL}? [y/N] " ANSWER
if [[ "$ANSWER" == "y" ]]; then
  az sql db delete --resource-group "$RG" --server "$SQL_SERVER" --name "$DRILL" --yes
fi
