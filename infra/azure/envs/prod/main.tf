module "bookstore" {
  source = "../../bookstore"

  environment = "prod"
  api = {
    image_name = "bookstore/bookstore-api"
    image_tag  = "1.1.0"
    sku_name   = "P0v3"
  }
  shared_key_vault = {
    name                = "kv-platform-prod-weu"
    resource_group_name = "rg-platform-prod-weu"
  }
}
