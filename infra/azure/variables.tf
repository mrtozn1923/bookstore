variable "environment" {
  type        = string
  description = "Deployment environment: dev, staging or prod."

  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "The environment must be one of: dev, staging, prod."
  }
}

variable "location" {
  type        = string
  description = "Azure region for every resource."
  default     = "westeurope"
}

variable "api" {
  type = object({
    image_name   = string
    image_tag    = string
    registry_url = optional(string, "https://ghcr.io")
    sku_name     = optional(string, "B1")
    app_settings = optional(map(string), {})
  })
  description = "Bookstore API: container image and App Service settings."
}

variable "allowed_ip_ranges" {
  type        = map(string)
  description = "Name => CIDR range allowed to reach the API. Empty map = no restriction."
  default     = {}
  nullable    = false
}

variable "storage_containers" {
  type        = set(string)
  description = "Blob containers in the Bookstore storage account."
  default     = ["covers", "exports"]
  nullable    = false
}

variable "sql_admin_login" {
  type        = string
  description = "SQL Server administrator login."
  default     = "bookstoreadmin"
}

variable "sql_admin_password" {
  type        = string
  description = "SQL Server administrator password (post 9 removes it from the state)."
  sensitive   = true
}
