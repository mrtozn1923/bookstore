# Runs without Azure: every azurerm resource and data source gets fake values
mock_provider "azurerm" {}

variables {
  name_suffix         = "bookstore-test-weu"
  resource_group_name = "rg-bookstore-test-weu"
  location            = "westeurope"
  image               = { name = "bookstore/bookstore-api", tag = "1.1.0" }
  key_vault = {
    id   = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-platform-test-weu/providers/Microsoft.KeyVault/vaults/kv-platform-test-weu"
    name = "kv-platform-test-weu"
  }
}

run "https_and_tls" {
  command = plan

  assert {
    condition     = azurerm_linux_web_app.this.https_only == true
    error_message = "The web app must accept HTTPS only."
  }

  assert {
    condition     = azurerm_linux_web_app.this.site_config[0].minimum_tls_version == "1.2"
    error_message = "The minimum TLS version must be 1.2."
  }
}

run "open_without_ip_ranges" {
  command = plan

  assert {
    condition     = azurerm_linux_web_app.this.site_config[0].ip_restriction_default_action == "Allow"
    error_message = "Without IP ranges the app must stay reachable."
  }
}

run "deny_by_default_with_ip_ranges" {
  command = plan

  variables {
    allowed_ip_ranges = { office = "203.0.113.0/24" }
  }

  assert {
    condition     = azurerm_linux_web_app.this.site_config[0].ip_restriction_default_action == "Deny"
    error_message = "With IP ranges, everything else must be denied."
  }

  assert {
    condition     = length(azurerm_linux_web_app.this.site_config[0].ip_restriction) == 1
    error_message = "Expected one IP restriction rule."
  }
}
