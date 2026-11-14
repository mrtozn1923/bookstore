provider "docker" {}

resource "docker_network" "bookstore" {
  name = local.name_prefix

  dynamic "labels" {
    for_each = local.labels
    content {
      label = labels.key
      value = labels.value
    }
  }
}

resource "docker_volume" "api_data" {
  for_each = var.volume_names
  name     = "${local.name_prefix}-${each.key}"

  dynamic "labels" {
    for_each = local.labels
    content {
      label = labels.key
      value = labels.value
    }
  }
}

resource "docker_container" "api" {
  name    = "${local.name_prefix}-api"
  image   = "bookstore-api:${var.api.image_tag}"
  restart = var.environment == "prod" ? "always" : "unless-stopped"

  env = concat(
    [for key, value in local.app_settings : "${key}=${value}"],
    ["Bookstore__Payments__ApiKey=${var.payments_api_key}"],
  )

  dynamic "labels" {
    for_each = local.labels
    content {
      label = labels.key
      value = labels.value
    }
  }

  dynamic "volumes" {
    for_each = docker_volume.api_data
    content {
      volume_name    = volumes.value.name
      container_path = "/data/${volumes.key}"
    }
  }

  networks_advanced {
    name = docker_network.bookstore.name
  }

  ports {
    internal = 8080
    external = var.api.external_port
  }
}
