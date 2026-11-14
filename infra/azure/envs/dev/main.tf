module "bookstore" {
  source = "../../bookstore"

  environment = "dev"
  api = {
    image_name = "bookstore/bookstore-api"
    image_tag  = "1.1.0"
    app_settings = {
      Logging__LogLevel__Default = "Information"
    }
  }
  allowed_ip_ranges = {
    office = "203.0.113.0/24"
  }
  shared_key_vault = {
    name                = "kv-platform-dev-weu"
    resource_group_name = "rg-platform-dev-weu"
  }
}
