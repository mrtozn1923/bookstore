terraform {
  required_version = "~> 1.17"

  backend "azurerm" {
    resource_group_name  = "rg-bookstore-tfstate-weu"
    storage_account_name = "stbookstoretfstateweu"
    container_name       = "tfstate"
    key                  = "bookstore/staging.tfstate"
    use_azuread_auth     = true
  }

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 5.14"
    }
    http = {
      source  = "hashicorp/http"
      version = "~> 3.6"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.9"
    }
  }
}
