output "api_url" {
  description = "Health endpoint of the local Bookstore API."
  value       = "http://localhost:${var.api.external_port}/health"
}

output "volume_names" {
  description = "Docker volume names, keyed by data set."
  value       = module.api.volume_names
}

output "app_settings" {
  description = "Non-secret settings passed to the API."
  value       = local.app_settings
}

output "subnet" {
  description = "This environment's /24 inside address_space."
  value       = local.subnet
}
