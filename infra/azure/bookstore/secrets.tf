# Generated in memory on every run; Terraform never writes it to state or plan
ephemeral "random_password" "sql_admin" {
  length           = 32
  special          = true
  override_special = "!#%*-_=+"
  min_lower        = 2
  min_upper        = 2
  min_numeric      = 2
  min_special      = 2
}

# Break-glass copy for people; the app connects with its managed identity, not this password
# tflint-ignore: azurerm_resources_missing_prevent_destroy # rotation replaces the value, not the secret
resource "azurerm_key_vault_secret" "sql_admin_password" {
  name             = "bookstore-sql-admin-password"
  key_vault_id     = data.azurerm_key_vault.shared.id
  content_type     = "password"
  value_wo         = ephemeral.random_password.sql_admin.result
  value_wo_version = var.sql_admin_password_version
}
