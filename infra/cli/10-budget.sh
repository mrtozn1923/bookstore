#!/usr/bin/env bash
# Monthly budget for everything tagged app=bookstore: an alert at 80% of actual cost and one at 100% forecast.
set -euo pipefail

: "${AZURE_SUBSCRIPTION_ID:?Set AZURE_SUBSCRIPTION_ID to the target subscription}"
AMOUNT="${1:?Usage: 10-budget.sh <monthly amount in the billing currency>}"
RG="rg-bookstore-prod-weu"
BUDGET="budget-bookstore-monthly"

az account set --subscription "$AZURE_SUBSCRIPTION_ID"

# Who hears about it: e-mail for now; a webhook or runbook can join the same group later
az monitor action-group create --name ag-bookstore-cost --resource-group "$RG" --short-name bscost \
  --action email finops finops@example.com
AG_ID=$(az monitor action-group show --name ag-bookstore-cost --resource-group "$RG" --query id --output tsv)

# "az consumption budget create" is in preview and can't set alerts, so the REST API is called directly
BODY=$(mktemp)
trap 'rm -f "$BODY"' EXIT
cat > "$BODY" <<JSON
{
  "properties": {
    "category": "Cost",
    "amount": ${AMOUNT},
    "timeGrain": "Monthly",
    "timePeriod": { "startDate": "$(date -u +%Y-%m-01)T00:00:00Z" },
    "filter": { "tags": { "name": "app", "operator": "In", "values": ["bookstore"] } },
    "notifications": {
      "actual-80": {
        "enabled": true, "operator": "GreaterThan", "threshold": 80, "thresholdType": "Actual",
        "contactGroups": ["${AG_ID}"], "locale": "en-us"
      },
      "forecast-100": {
        "enabled": true, "operator": "GreaterThan", "threshold": 100, "thresholdType": "Forecasted",
        "contactGroups": ["${AG_ID}"], "locale": "en-us"
      }
    }
  }
}
JSON

az rest --method put --body "@${BODY}" \
  --url "https://management.azure.com/subscriptions/${AZURE_SUBSCRIPTION_ID}/providers/Microsoft.Consumption/budgets/${BUDGET}?api-version=2026-06-01"
