# OpenTofu only: Terraform ignores .tofu files
terraform {
  required_version = ">= 1.11"

  required_providers {
    docker = {
      source  = "kreuzwerker/docker"
      version = "~> 4.7"
    }
  }

  # Client-side state and plan encryption; the passphrase never lands in a file
  encryption {
    key_provider "pbkdf2" "passphrase" {
      passphrase = var.state_passphrase
    }

    method "aes_gcm" "default" {
      keys = key_provider.pbkdf2.passphrase
    }

    state {
      method = method.aes_gcm.default
    }

    plan {
      method = method.aes_gcm.default
    }
  }
}

variable "state_passphrase" {
  type      = string
  sensitive = true
}

variable "with_reports" {
  type    = bool
  default = false
}

provider "docker" {}

resource "docker_volume" "covers" {
  name = "tfseries-tofu-covers"
}

# One or zero instances without count: the address stays docker_volume.reports
resource "docker_volume" "reports" {
  name = "tfseries-tofu-reports"

  lifecycle {
    enabled = var.with_reports
  }
}
