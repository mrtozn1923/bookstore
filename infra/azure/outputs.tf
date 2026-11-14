output "api_url" {
  description = "Public URL of the Bookstore API."
  value       = module.api.url
}

output "sql_server_fqdn" {
  description = "Host name for the BookstoreDb connection string."
  value       = azurerm_mssql_server.bookstore.fully_qualified_domain_name
}

output "storage_containers" {
  description = "Blob container names, keyed by data set."
  value       = { for key, container in azurerm_storage_container.bookstore : key => container.name }
}
