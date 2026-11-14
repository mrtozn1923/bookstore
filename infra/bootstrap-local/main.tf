# This stack keeps its own state locally: it creates the backend the other stacks use.
terraform {
  required_version = "~> 1.17"

  required_providers {
    docker = {
      source  = "kreuzwerker/docker"
      version = "~> 4.7"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.9"
    }
  }
}

provider "docker" {}

resource "random_password" "state_db" {
  length  = 24
  special = false
}

resource "docker_image" "postgres" {
  name         = "postgres:18-alpine"
  keep_locally = true
}

resource "docker_volume" "state_db" {
  name = "tfseries-state-data"
}

resource "docker_container" "state_db" {
  name    = "tfseries-state"
  image   = docker_image.postgres.image_id
  restart = "unless-stopped"
  wait    = true

  env = [
    "POSTGRES_USER=terraform",
    "POSTGRES_PASSWORD=${random_password.state_db.result}",
    "POSTGRES_DB=terraform_state",
  ]

  healthcheck {
    test     = ["CMD", "pg_isready", "-U", "terraform", "-d", "terraform_state"]
    interval = "2s"
    retries  = 15
  }

  volumes {
    volume_name    = docker_volume.state_db.name
    container_path = "/var/lib/postgresql"
  }

  ports {
    internal = 5432
    external = 15480
  }
}

output "pg_conn_str" {
  description = "Connection string for the pg backend (PG_CONN_STR)."
  value       = "postgres://terraform:${random_password.state_db.result}@localhost:15480/terraform_state?sslmode=disable"
  sensitive   = true
}
