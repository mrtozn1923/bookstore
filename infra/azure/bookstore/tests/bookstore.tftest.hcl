# Plans and "applies" against mocks only; nothing is created in Azure
mock_provider "azurerm" {
  mock_data "azurerm_key_vault" {
    defaults = {
      id = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-platform-test-weu/providers/Microsoft.KeyVault/vaults/kv-platform-test-weu"
    }
  }

  # The provider still validates IDs during apply, so random mock strings are not enough
  mock_resource "azurerm_storage_account" {
    defaults = {
      id = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-bookstore-dev-weu/providers/Microsoft.Storage/storageAccounts/stbookstoredevweu"
    }
  }

  mock_resource "azurerm_mssql_server" {
    defaults = {
      id = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-bookstore-dev-weu/providers/Microsoft.Sql/servers/sql-bookstore-dev-weu"
    }
  }

  mock_resource "azurerm_service_plan" {
    defaults = {
      id = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-bookstore-dev-weu/providers/Microsoft.Web/serverFarms/asp-bookstore-dev-weu"
    }
  }

  mock_resource "azurerm_user_assigned_identity" {
    defaults = {
      id           = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-bookstore-dev-weu/providers/Microsoft.ManagedIdentity/userAssignedIdentities/id-bookstore-dev-weu"
      principal_id = "11111111-1111-1111-1111-111111111111"
    }
  }
}

mock_provider "http" {
  mock_data "http" {
    defaults = {
      status_code = 200
    }
  }
}

variables {
  environment = "dev"
  api = {
    image_name = "bookstore/bookstore-api"
    image_tag  = "1.1.0"
  }
  shared_key_vault = {
    name                = "kv-platform-test-weu"
    resource_group_name = "rg-platform-test-weu"
  }
}

run "dev_uses_small_tiers" {
  command = apply

  assert {
    condition     = azurerm_storage_account.bookstore.account_replication_type == "LRS"
    error_message = "dev should use LRS storage."
  }

  assert {
    condition     = azurerm_mssql_database.bookstore.sku_name == "Basic"
    error_message = "dev should use the Basic database tier."
  }
}

run "prod_rejects_b1" {
  command = plan

  variables {
    environment = "prod"
  }

  expect_failures = [var.api]
}

run "prod_uses_zone_redundancy" {
  command = plan

  variables {
    environment = "prod"
    api = {
      image_name = "bookstore/bookstore-api"
      image_tag  = "1.1.0"
      sku_name   = "P0v3"
    }
  }

  assert {
    condition     = azurerm_storage_account.bookstore.account_replication_type == "ZRS"
    error_message = "prod storage must be zone-redundant."
  }

  assert {
    condition     = azurerm_mssql_database.bookstore.sku_name == "S1"
    error_message = "prod should use the S1 database tier."
  }
}
