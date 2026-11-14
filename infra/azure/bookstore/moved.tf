# The web tier moved into the bookstore-webapp module (post 8 covers moved blocks in detail)
moved {
  from = azurerm_service_plan.bookstore
  to   = module.api.azurerm_service_plan.this
}

moved {
  from = azurerm_user_assigned_identity.api
  to   = module.api.azurerm_user_assigned_identity.this
}

moved {
  from = azurerm_role_assignment.api_key_vault
  to   = module.api.azurerm_role_assignment.key_vault_secrets
}

moved {
  from = azurerm_linux_web_app.api
  to   = module.api.azurerm_linux_web_app.this
}
