provider "docker" {}

resource "docker_network" "bookstore" {
  name = "tfseries-dev"
}

resource "docker_container" "api" {
  name    = "tfseries-dev-api"
  image   = "bookstore-api:1.1.0"
  restart = "unless-stopped"

  env = ["Bookstore__Environment=dev"]

  networks_advanced {
    name = docker_network.bookstore.name
  }

  ports {
    internal = 8080
    external = 18480
  }
}
