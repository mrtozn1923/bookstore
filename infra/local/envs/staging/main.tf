provider "docker" {}

module "bookstore" {
  source = "../../bookstore"

  environment = "staging"
  api = {
    image_tag     = "1.1.0"
    external_port = 18481
  }
  payments_api_key = var.payments_api_key
}
