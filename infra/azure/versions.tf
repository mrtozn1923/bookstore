terraform {
  required_version = "~> 1.17"

  # Partial configuration: the rest comes from -backend-config=backend/<environment>.azurerm.tfbackend
  backend "azurerm" {
    use_azuread_auth = true
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
  }
}
