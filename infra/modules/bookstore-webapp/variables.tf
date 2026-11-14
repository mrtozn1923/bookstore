variable "name_suffix" {
  type        = string
  description = "Shared naming suffix, e.g. bookstore-dev-weu; resources become asp-, id-, app-<suffix>."
}

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "sku_name" {
  type        = string
  description = "App Service Plan SKU."
  default     = "B1"
}

variable "image" {
  type = object({
    name         = string
    tag          = string
    registry_url = optional(string, "https://ghcr.io")
  })
  description = "Initial container image; later tags are set by the deployment pipeline."
}

variable "app_settings" {
  type    = map(string)
  default = {}
}

variable "allowed_ip_ranges" {
  type        = map(string)
  description = "Name => CIDR range allowed to reach the app. Empty map = no restriction."
  default     = {}
  nullable    = false
}

variable "key_vault" {
  type = object({
    id   = string
    name = string
  })
  description = "Key Vault the app reads secrets from with its managed identity."
}

variable "health_check_path" {
  type    = string
  default = "/health"
}

variable "tags" {
  type    = map(string)
  default = {}
}
