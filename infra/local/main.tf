provider "docker" {}

data "docker_network" "shared" {
  name = var.shared_network_name

  lifecycle {
    postcondition {
      condition     = self.driver == "bridge"
      error_message = "The shared network must use the bridge driver."
    }
  }
}

data "docker_image" "api" {
  name = "bookstore-api:${var.api.image_tag}"
}

# Rebuilding the same tag changes the image ID; this resource carries that change
resource "terraform_data" "api_image" {
  input = data.docker_image.api.id
}

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

  lifecycle {
    prevent_destroy = true
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

  networks_advanced {
    name = data.docker_network.shared.name
  }

  ports {
    internal = 8080
    external = var.api.external_port
  }

  lifecycle {
    replace_triggered_by = [terraform_data.api_image]

    # Memory limits are tuned by the operations team with docker update
    ignore_changes = [memory, memory_swap]

    precondition {
      condition     = var.environment != "prod" || var.api.image_tag != "latest"
      error_message = "Production must run a pinned image tag, not latest."
    }
  }
}

check "api_health" {
  data "http" "health" {
    url = "http://localhost:${var.api.external_port}/health"

    # The container needs a few seconds to start after a replace
    retry {
      attempts     = 10
      min_delay_ms = 500
      max_delay_ms = 2000
    }
  }

  assert {
    condition     = data.http.health.status_code == 200
    error_message = "The Bookstore API health endpoint returned ${data.http.health.status_code}."
  }
}
