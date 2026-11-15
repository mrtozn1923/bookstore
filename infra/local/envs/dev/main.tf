provider "docker" {}

module "bookstore" {
  source = "../../bookstore"

  environment = "dev"
  api = {
    image_tag     = "1.1.0"
    external_port = 18480
    settings      = yamldecode(file("${path.module}/appsettings.yaml"))
  }
  payments_api_key = var.payments_api_key
}
