output "api_url" {
  description = "Health endpoint of the local Bookstore API."
  value       = module.bookstore.api_url
}

output "volume_names" {
  description = "Docker volume names, keyed by data set."
  value       = module.bookstore.volume_names
}
