locals {
  env = concat(
    [for key, value in merge(var.environment_variables, var.env) : "${key}=${value}"],
    [for key, value in var.secret_env : "${key}=${value}"],
  )
}

data "docker_image" "this" {
  name = var.image
}

# Rebuilding the same tag changes the image ID; this resource carries that change
resource "terraform_data" "image" {
  input = data.docker_image.this.id
}

resource "docker_volume" "data" {
  for_each = var.data_volumes
  name     = "${coalesce(var.volume_prefix, var.name)}-${each.key}"

  dynamic "labels" {
    for_each = var.labels
    content {
      label = labels.key
      value = labels.value
    }
  }

  lifecycle {
    prevent_destroy = true
  }
}

resource "docker_container" "this" {
  name    = var.name
  image   = var.image
  restart = var.restart
  env     = local.env

  dynamic "labels" {
    for_each = var.labels
    content {
      label = labels.key
      value = labels.value
    }
  }

  dynamic "volumes" {
    for_each = docker_volume.data
    content {
      volume_name    = volumes.value.name
      container_path = "/data/${volumes.key}"
    }
  }

  dynamic "networks_advanced" {
    for_each = var.networks
    content {
      name = networks_advanced.value
    }
  }

  ports {
    internal = var.internal_port
    external = var.external_port
  }

  lifecycle {
    replace_triggered_by = [terraform_data.image]

    # Memory limits are tuned by the operations team with docker update
    ignore_changes = [memory, memory_swap]
  }
}
