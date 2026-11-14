locals {
  region_short = {
    westeurope    = "weu"
    northeurope   = "neu"
    swedencentral = "sdc"
  }

  # <type>-bookstore-<environment>-<region>, e.g. app-bookstore-dev-weu
  suffix         = "bookstore-${var.environment}-${lookup(local.region_short, var.location, var.location)}"
  compact_suffix = replace(local.suffix, "-", "")

  tags = {
    app         = "bookstore"
    environment = var.environment
    managed_by  = "terraform"
  }

  app_settings = merge(
    { Bookstore__Environment = var.environment },
    var.api.app_settings,
  )
}
