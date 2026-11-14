terraform {
  required_version = ">= 1.15"

  required_providers {
    docker = {
      source  = "kreuzwerker/docker"
      version = ">= 4.0, < 5.0"
    }
    http = {
      source  = "hashicorp/http"
      version = ">= 3.4, < 4.0"
    }
  }
}
