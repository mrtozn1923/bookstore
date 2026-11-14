environment = "dev"

api = {
  image_tag     = "1.1.0"
  external_port = 18480
  app_settings = {
    Logging__LogLevel__Default = "Information"
  }
}
