resource "azurerm_resource_group" "bookstore" {
  name     = "rg-bookstore-dev-weu"
  location = "westeurope"

  tags = {
    app         = "bookstore"
    environment = "dev"
    managed_by  = "terraform"
  }
}

resource "azurerm_storage_account" "covers" {
  name                     = "stbookstoredevweu"
  resource_group_name      = azurerm_resource_group.bookstore.name
  location                 = azurerm_resource_group.bookstore.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  min_tls_version          = "TLS1_2"

  tags = azurerm_resource_group.bookstore.tags
}
