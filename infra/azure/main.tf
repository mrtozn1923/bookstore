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

resource "azurerm_service_plan" "bookstore" {
  name                = "asp-${local.suffix}"
  resource_group_name = azurerm_resource_group.bookstore.name
  location            = azurerm_resource_group.bookstore.location
  os_type             = "Linux"
  sku_name            = var.api.sku_name
  tags                = local.tags
}

resource "azurerm_linux_web_app" "api" {
  name                = "app-${local.suffix}"
  resource_group_name = azurerm_resource_group.bookstore.name
  location            = azurerm_resource_group.bookstore.location
  service_plan_id     = azurerm_service_plan.bookstore.id
  https_only          = true
  app_settings        = local.app_settings
  tags                = local.tags

  site_config {
    minimum_tls_version               = "1.2"
    health_check_path                 = "/health"
    health_check_eviction_time_in_min = 5
    ip_restriction_default_action     = length(var.allowed_ip_ranges) > 0 ? "Deny" : "Allow"

    application_stack {
      docker_image_name   = "${var.api.image_name}:${var.api.image_tag}"
      docker_registry_url = var.api.registry_url
    }

    dynamic "ip_restriction" {
      for_each = var.allowed_ip_ranges
      content {
        name       = ip_restriction.key
        ip_address = ip_restriction.value
        action     = "Allow"
        priority   = 100
      }
    }
  }
}

resource "azurerm_mssql_server" "bookstore" {
  name                         = "sql-${local.suffix}"
  resource_group_name          = azurerm_resource_group.bookstore.name
  location                     = azurerm_resource_group.bookstore.location
  version                      = "12.0"
  administrator_login          = var.sql_admin_login
  administrator_login_password = var.sql_admin_password
  minimum_tls_version          = "1.2"
  tags                         = local.tags
}

resource "azurerm_mssql_database" "bookstore" {
  name      = "BookstoreDb"
  server_id = azurerm_mssql_server.bookstore.id
  sku_name  = var.environment == "prod" ? "S1" : "Basic"
  tags      = local.tags
}
