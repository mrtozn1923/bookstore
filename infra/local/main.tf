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

module "api" {
  source = "../modules/bookstore-service"

  name          = "${local.name_prefix}-api"
  volume_prefix = local.name_prefix
  image         = "bookstore-api:${var.api.image_tag}"
  restart       = var.environment == "prod" ? "always" : "unless-stopped"
  env           = local.app_settings
  secret_env    = { Bookstore__Payments__ApiKey = var.payments_api_key }
  networks      = [docker_network.bookstore.name, data.docker_network.shared.name]
  data_volumes  = var.volume_names
  external_port = var.api.external_port
  labels        = local.labels
}

check "api_health" {
  data "http" "health" {
    url = "${module.api.base_url}/health"

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
