resource "azurerm_resource_group" "bookstore" {
  name     = "rg-${local.suffix}"
  location = var.location
  tags     = local.tags
}

resource "azurerm_storage_account" "bookstore" {
  name                     = "st${local.compact_suffix}"
  resource_group_name      = azurerm_resource_group.bookstore.name
  location                 = azurerm_resource_group.bookstore.location
  account_tier             = "Standard"
  account_replication_type = var.environment == "prod" ? "ZRS" : "LRS"
  min_tls_version          = "TLS1_2"
  tags                     = local.tags
}

resource "azurerm_storage_container" "bookstore" {
  for_each = var.storage_containers

  name                  = each.key
  storage_account_id    = azurerm_storage_account.bookstore.id
  container_access_type = "private"
}

data "azurerm_key_vault" "shared" {
  name                = var.shared_key_vault.name
  resource_group_name = var.shared_key_vault.resource_group_name
}

module "api" {
  source = "../../modules/bookstore-webapp"

  name_suffix         = local.suffix
  resource_group_name = azurerm_resource_group.bookstore.name
  location            = azurerm_resource_group.bookstore.location
  sku_name            = var.api.sku_name
  image               = { name = var.api.image_name, tag = var.api.image_tag, registry_url = var.api.registry_url }
  allowed_ip_ranges   = var.allowed_ip_ranges
  key_vault           = { id = data.azurerm_key_vault.shared.id, name = data.azurerm_key_vault.shared.name }
  tags                = local.tags

  app_settings = local.app_settings
}

resource "azurerm_mssql_server" "bookstore" {
  name                = "sql-${local.suffix}"
  resource_group_name = azurerm_resource_group.bookstore.name
  location            = azurerm_resource_group.bookstore.location
  version             = "12.0"
  administrator_login = var.sql_admin_login
  minimum_tls_version = "1.2"
  tags                = local.tags

  # Write-only: sent to Azure only when the version changes, never stored in state or plan
  administrator_login_password_wo         = ephemeral.random_password.sql_admin.result
  administrator_login_password_wo_version = var.sql_admin_password_version
}

resource "azurerm_mssql_database" "bookstore" {
  name      = "BookstoreDb"
  server_id = azurerm_mssql_server.bookstore.id
  sku_name  = var.environment == "prod" ? "S1" : "Basic"
  tags      = local.tags

  lifecycle {
    prevent_destroy = true
  }
}

check "api_health" {
  data "http" "health" {
    url = "${module.api.url}/health"
  }

  assert {
    condition     = data.http.health.status_code == 200
    error_message = "The Bookstore API health endpoint returned ${data.http.health.status_code}."
  }
}
