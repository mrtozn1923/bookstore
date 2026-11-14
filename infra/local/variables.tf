variable "environment" {
  type        = string
  description = "Deployment environment: dev, staging or prod."

  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "The environment must be one of: dev, staging, prod."
  }
}

variable "api" {
  type = object({
    image_tag     = string
    external_port = number
    app_settings  = optional(map(string), {})
  })
  description = "Bookstore API container settings."

  validation {
    condition     = var.api.external_port >= 1024 && var.api.external_port <= 65535
    error_message = "The external_port must be between 1024 and 65535."
  }
}

variable "volume_names" {
  type        = set(string)
  description = "Data volumes mounted under /data in the API container."
  default     = ["covers", "exports"]
  nullable    = false
}

variable "payments_api_key" {
  type        = string
  description = "Key for the payment provider's sandbox."
  sensitive   = true
}

variable "shared_network_name" {
  type        = string
  description = "Network owned by the platform team; read here, never managed."
  default     = "tfseries-shared"
}
