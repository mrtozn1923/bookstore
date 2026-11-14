variable "name" {
  type        = string
  description = "Container name."
}

variable "volume_prefix" {
  type        = string
  description = "Data volumes are named <volume_prefix>-<data set>; defaults to name."
  default     = null
}

variable "image" {
  type        = string
  description = "Image reference with a pinned tag, e.g. bookstore-api:1.1.0."

  validation {
    condition     = !endswith(var.image, ":latest")
    error_message = "Use a pinned image tag, not latest."
  }
}

variable "env" {
  type        = map(string)
  description = "Non-secret environment variables."
  default     = {}
  nullable    = false
}

variable "environment_variables" {
  type        = map(string)
  description = "Old name of env."
  default     = {}
  nullable    = false
  deprecated  = "Use env instead; environment_variables will be removed in the next major version of this module."
}

variable "secret_env" {
  type        = map(string)
  description = "Secret environment variables (still stored in the state)."
  default     = {}
  sensitive   = true
}

variable "networks" {
  type        = list(string)
  description = "Networks to attach the container to."
}

variable "data_volumes" {
  type        = set(string)
  description = "Data sets that get a volume mounted at /data/<name>."
  default     = []
}

variable "internal_port" {
  type    = number
  default = 8080
}

variable "external_port" {
  type        = number
  description = "Host port mapped to internal_port."
}

variable "restart" {
  type    = string
  default = "unless-stopped"
}

variable "labels" {
  type    = map(string)
  default = {}
}
