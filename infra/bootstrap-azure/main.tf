# Creates the storage account that holds Bookstore's Terraform state.
# This stack keeps its own state locally (or is run once by the platform team).
terraform {
  required_version = "~> 1.17"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 5.14"
    }
  }
}

provider "azurerm" {
  resource_providers_to_register = ["Microsoft.Storage"]
  features {}
}

variable "state_writer_principal_ids" {
  type        = set(string)
  description = "Object IDs (people, CI identities) allowed to read and write state."
  default     = []
}

resource "azurerm_resource_group" "tfstate" {
  name     = "rg-bookstore-tfstate-weu"
  location = "westeurope"

  tags = {
    app        = "bookstore"
    purpose    = "terraform-state"
    managed_by = "terraform"
  }
}

resource "azurerm_storage_account" "tfstate" {
  name                            = "stbookstoretfstateweu"
  resource_group_name             = azurerm_resource_group.tfstate.name
  location                        = azurerm_resource_group.tfstate.location
  account_tier                    = "Standard"
  account_replication_type        = "ZRS"
  min_tls_version                 = "TLS1_2"
  shared_access_key_enabled       = false
  default_to_oauth_authentication = true
  tags                            = azurerm_resource_group.tfstate.tags

  blob_properties {
    versioning_enabled = true

    delete_retention_policy {
      days = 30
    }

    container_delete_retention_policy {
      days = 30
    }
  }
}

resource "azurerm_storage_container" "tfstate" {
  name                  = "tfstate"
  storage_account_id    = azurerm_storage_account.tfstate.id
  container_access_type = "private"
}

resource "azurerm_role_assignment" "state_writers" {
  for_each = var.state_writer_principal_ids

  scope                = azurerm_storage_account.tfstate.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = each.key
}
