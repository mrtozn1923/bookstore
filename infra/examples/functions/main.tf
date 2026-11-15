# A playground for built-in functions: no provider, no state; run terraform console here
terraform {
  required_version = "~> 1.17"
}

variable "environment" {
  type    = string
  default = "dev"
}

variable "image" {
  type    = string
  default = "bookstore-api:1.1.0"
}

locals {
  environments = ["dev", "staging", "prod"]
  data_sets    = ["covers", "exports"]

  # Days to keep blobs per data set
  retention = zipmap(local.data_sets, [365, 30])

  settings = yamldecode(file("${path.module}/appsettings.yaml"))
}
