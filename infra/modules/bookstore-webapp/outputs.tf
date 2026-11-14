output "url" {
  description = "Public HTTPS URL of the app."
  value       = "https://${azurerm_linux_web_app.this.default_hostname}"
}

output "web_app_id" {
  value = azurerm_linux_web_app.this.id
}

output "identity_principal_id" {
  description = "Principal ID of the app's managed identity, for extra role assignments."
  value       = azurerm_user_assigned_identity.this.principal_id
}
