resource "azurerm_service_plan" "this" {
  name                = "asp-${var.name_suffix}"
  resource_group_name = var.resource_group_name
  location            = var.location
  os_type             = "Linux"
  sku_name            = var.sku_name
  tags                = var.tags
}

resource "azurerm_user_assigned_identity" "this" {
  name                = "id-${var.name_suffix}"
  resource_group_name = var.resource_group_name
  location            = var.location
  tags                = var.tags
}

resource "azurerm_role_assignment" "key_vault_secrets" {
  scope                = var.key_vault.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_user_assigned_identity.this.principal_id
}

resource "azurerm_linux_web_app" "this" {
  name                = "app-${var.name_suffix}"
  resource_group_name = var.resource_group_name
  location            = var.location
  service_plan_id     = azurerm_service_plan.this.id
  https_only          = true
  tags                = var.tags

  app_settings = merge(var.app_settings, {
    Bookstore__Payments__ApiKey = "@Microsoft.KeyVault(VaultName=${var.key_vault.name};SecretName=payments-api-key)"
  })

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.this.id]
  }

  key_vault_reference_identity_id = azurerm_user_assigned_identity.this.id

  site_config {
    minimum_tls_version               = "1.2"
    http2_enabled                     = true
    health_check_path                 = var.health_check_path
    health_check_eviction_time_in_min = 5
    ip_restriction_default_action     = length(var.allowed_ip_ranges) > 0 ? "Deny" : "Allow"

    application_stack {
      docker_image_name   = "${var.image.name}:${var.image.tag}"
      docker_registry_url = var.image.registry_url
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
  depends_on = [azurerm_role_assignment.key_vault_secrets]

  lifecycle {
    # The deployment pipeline sets the image tag (CI/CD series); Terraform owns everything else
    ignore_changes = [site_config[0].application_stack[0].docker_image_name]
  }
}
