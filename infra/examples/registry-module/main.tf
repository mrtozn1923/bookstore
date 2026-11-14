# Example only: the same web app from an Azure Verified Module instead of our own module
terraform {
  required_version = "~> 1.17"

  required_providers {
    azapi = {
      source  = "Azure/azapi"
      version = "~> 2.12"
    }
  }
}

provider "azapi" {}

module "web" {
  source  = "Azure/avm-res-web-site/azurerm"
  version = "0.24.1"

  name                     = "app-bookstore-dev-weu"
  location                 = "westeurope"
  parent_id                = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-bookstore-dev-weu"
  service_plan_resource_id = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-bookstore-dev-weu/providers/Microsoft.Web/serverFarms/asp-bookstore-dev-weu"
  enable_telemetry         = false
}
