module "bookstore" {
  source = "../../bookstore"

  environment = "staging"
  api = {
    image_name = "bookstore/bookstore-api"
    image_tag  = "1.1.0"
  }
  allowed_ip_ranges = {
    office = "203.0.113.0/24"
  }
  shared_key_vault = {
    name                = "kv-platform-staging-weu"
    resource_group_name = "rg-platform-staging-weu"
  }
}
