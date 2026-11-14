# Lists what was built by hand in the portal for staging
list "azurerm_resource_group" "all" {
  provider = azurerm
  config {}
}

list "azurerm_storage_account" "staging" {
  provider = azurerm
  config {
    resource_group_name = "rg-bookstore-staging-weu"
  }
}

list "azurerm_mssql_server" "staging" {
  provider = azurerm
  config {
    resource_group_name = "rg-bookstore-staging-weu"
  }
}
