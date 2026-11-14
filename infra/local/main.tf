terraform {
  required_providers {
    docker = {
      source  = "kreuzwerker/docker"
      version = "~> 4.7"
    }
  }
}

provider "docker" {}

resource "docker_network" "bookstore" {
  name = "tfseries-dev"
}

resource "docker_container" "api" {
  name  = "tfseries-dev-api"
  image = "bookstore-api:1.0.0"

  env = ["Bookstore__Environment=dev"]

  networks_advanced {
    name = docker_network.bookstore.name
  }

  ports {
    internal = 8080
    external = 18480
  }
}
