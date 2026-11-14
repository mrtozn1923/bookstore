locals {
  name_prefix = "tfseries-${var.environment}"

  labels = {
    app         = "bookstore"
    environment = var.environment
    managed_by  = "terraform"
  }

  app_settings = merge(
    { Bookstore__Environment = var.environment },
    var.api.app_settings,
  )
}
