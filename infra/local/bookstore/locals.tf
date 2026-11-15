locals {
  name_prefix = "tfseries-${var.environment}"

  labels = {
    app         = "bookstore"
    environment = var.environment
    managed_by  = "terraform"
  }

  # Fixed numbers, not list positions: adding an environment never moves another one's subnet
  subnet_numbers = {
    dev     = 1
    staging = 2
    prod    = 3
  }
  subnet = cidrsubnet(var.address_space, 8, local.subnet_numbers[var.environment])

  # { Logging = { LogLevel__Default = "Information" } } => { Logging__LogLevel__Default = "Information" }
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
