output "api_url" {
  description = "Public URL of the Bookstore API."
  value       = module.bookstore.api_url
}

output "sql_server_fqdn" {
  description = "Host name for the BookstoreDb connection string."
  value       = module.bookstore.sql_server_fqdn
}
