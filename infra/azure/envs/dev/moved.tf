# The dev environment used to be the root module in infra/azure
moved {
  from = azurerm_resource_group.bookstore
  to   = module.bookstore.azurerm_resource_group.bookstore
}

moved {
  from = azurerm_storage_account.bookstore
  to   = module.bookstore.azurerm_storage_account.bookstore
}

moved {
  from = azurerm_storage_container.bookstore
  to   = module.bookstore.azurerm_storage_container.bookstore
}

moved {
  from = module.api
  to   = module.bookstore.module.api
}

moved {
  from = azurerm_mssql_server.bookstore
  to   = module.bookstore.azurerm_mssql_server.bookstore
}

moved {
  from = azurerm_mssql_database.bookstore
  to   = module.bookstore.azurerm_mssql_database.bookstore
}
