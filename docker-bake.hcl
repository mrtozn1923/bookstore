variable "TAG" {
  default = "1.2.0"
}

variable "REGISTRY" {
  default = "localhost:5000"
}

group "default" {
  targets = ["tools", "api", "web"]
}

group "ci" {
  targets = ["lint", "test", "tools", "api", "web"]
}

target "tools" {
  context    = "."
  dockerfile = "src/Bookstore.Tools/Dockerfile"
  target     = "final"
  args = {
    APP_VERSION = TAG
  }
  tags = ["dockerseries-bookstore-tools:${TAG}"]
}

target "api" {
  context    = "."
  dockerfile = "src/Bookstore.Api/Dockerfile"
  target     = "final"
  args = {
    APP_VERSION = TAG
  }
  tags = ["dockerseries-bookstore-api:${TAG}"]
}

target "web" {
  context = "web"
  target  = "final"
  tags    = ["dockerseries-bookstore-web:${TAG}"]
}

target "test" {
  context    = "."
  dockerfile = "src/Bookstore.Tools/Dockerfile"
  target     = "test"
  output     = ["type=cacheonly"]
}

target "lint" {
  name     = "lint-${app}"
  matrix   = { app = ["tools", "api", "web"] }
  inherits = [app]
  call     = "check"
}

target "tools-release" {
  inherits   = ["tools"]
  platforms  = ["linux/amd64", "linux/arm64"]
  tags       = ["${REGISTRY}/bookstore/tools:${TAG}"]
  cache-from = ["type=registry,ref=${REGISTRY}/bookstore/tools:buildcache"]
  cache-to   = ["type=registry,ref=${REGISTRY}/bookstore/tools:buildcache,mode=max"]
  output     = ["type=registry"]
}
