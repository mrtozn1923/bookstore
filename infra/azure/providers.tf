provider "azurerm" {
  # Uses the account signed in with the Az CLI; the subscription ID comes from ARM_SUBSCRIPTION_ID
  resource_providers_to_register = ["Microsoft.Storage", "Microsoft.Web", "Microsoft.Sql"]

  features {}
}
