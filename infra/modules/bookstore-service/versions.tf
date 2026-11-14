terraform {
  required_version = ">= 1.15"

  required_providers {
    docker = {
      source  = "kreuzwerker/docker"
      version = ">= 4.0, < 5.0"
    }
  }
}
