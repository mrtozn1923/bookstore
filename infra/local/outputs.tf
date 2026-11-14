output "api_url" {
  description = "Health endpoint of the local Bookstore API."
  value       = "http://localhost:${var.api.external_port}/health"
}

output "volume_names" {
  description = "Docker volume names, keyed by data set."
  value       = { for key, volume in docker_volume.api_data : key => volume.name }
}

output "app_settings" {
  description = "Non-secret settings passed to the API."
  value       = local.app_settings
}
