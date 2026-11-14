output "container_name" {
  description = "Name of the running container."
  value       = docker_container.this.name
}

output "base_url" {
  description = "Base URL on the host."
  value       = "http://localhost:${var.external_port}"
}

output "volume_names" {
  description = "Volume names, keyed by data set."
  value       = { for key, volume in docker_volume.data : key => volume.name }
}

output "image_id" {
  description = "ID of the image the container runs."
  value       = data.docker_image.this.id
}

output "url" {
  description = "Old name of base_url."
  value       = "http://localhost:${var.external_port}"
  deprecated  = "Use base_url instead; url will be removed in the next major version of this module."
}
