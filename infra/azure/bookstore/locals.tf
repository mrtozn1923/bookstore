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

  # Same Section__Key flattening as infra/local: App Service on Linux reads the same names
  file_settings = merge([
    for section, values in var.api.settings : {
      for key, value in values : "${section}__${key}" => value
    }
  ]...)

  app_settings = merge(
    { Bookstore__Environment = var.environment },
    local.file_settings,
    var.api.app_settings,
  )
}
