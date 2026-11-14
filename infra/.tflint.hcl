plugin "terraform" {
  enabled = true
  preset  = "recommended"
}

plugin "azurerm" {
  enabled = true
  version = "0.33.0"
  source  = "github.com/terraform-linters/tflint-ruleset-azurerm"
}

# The health check (health_check_path) covers this for Bookstore
rule "azurerm_app_service_missing_auto_heal_setting" {
  enabled = false
}
