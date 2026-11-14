terraform {
  required_version = "~> 1.17"

  # The connection string comes from the PG_CONN_STR environment variable
  backend "pg" {
    schema_name = "bookstore_dev"
  }

  required_providers {
    docker = {
      source  = "kreuzwerker/docker"
      version = "~> 4.7"
    }
    http = {
      source  = "hashicorp/http"
      version = "~> 3.6"
    }
  }
}
