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

data "azurerm_key_vault" "shared" {
  name                = var.shared_key_vault.name
  resource_group_name = var.shared_key_vault.resource_group_name
}

resource "azurerm_user_assigned_identity" "api" {
  name                = "id-${local.suffix}"
  resource_group_name = azurerm_resource_group.bookstore.name
  location            = azurerm_resource_group.bookstore.location
  tags                = local.tags
}

resource "azurerm_role_assignment" "api_key_vault" {
  scope                = data.azurerm_key_vault.shared.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_user_assigned_identity.api.principal_id
}

resource "azurerm_linux_web_app" "api" {
  name                = "app-${local.suffix}"
  resource_group_name = azurerm_resource_group.bookstore.name
  location            = azurerm_resource_group.bookstore.location
  service_plan_id     = azurerm_service_plan.bookstore.id
  https_only          = true
  tags                = local.tags

  app_settings = merge(local.app_settings, {
    Bookstore__Payments__ApiKey = "@Microsoft.KeyVault(VaultName=${data.azurerm_key_vault.shared.name};SecretName=payments-api-key)"
  })

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.api.id]
  }

  key_vault_reference_identity_id = azurerm_user_assigned_identity.api.id

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

  # The app reads secrets at startup; nothing in this block references the role assignment
  depends_on = [azurerm_role_assignment.api_key_vault]

  lifecycle {
    # The deployment pipeline sets the image tag (CI/CD series); Terraform owns everything else
    ignore_changes = [site_config[0].application_stack[0].docker_image_name]

    precondition {
      condition     = var.environment != "prod" || var.api.sku_name != "B1"
      error_message = "Production needs at least a P0v3 plan, not B1."
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

  lifecycle {
    prevent_destroy = true
  }
}

check "api_health" {
  data "http" "health" {
    url = "https://${azurerm_linux_web_app.api.default_hostname}/health"
  }

  assert {
    condition     = data.http.health.status_code == 200
    error_message = "The Bookstore API health endpoint returned ${data.http.health.status_code}."
  }
}
