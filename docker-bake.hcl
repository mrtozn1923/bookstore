variable "TAG" {
  default = "1.0.0"
}

variable "REGISTRY" {
  default = "localhost:5000"
}

group "default" {
  targets = ["tools"]
}

group "ci" {
  targets = ["lint", "tools"]
}

target "tools" {
  context    = "."
  dockerfile = "src/Bookstore.Tools/Dockerfile"
  args = {
    APP_VERSION = TAG
  }
  tags = ["dockerseries-bookstore-tools:${TAG}"]
}

target "lint" {
  inherits = ["tools"]
  call     = "check"
}

target "tools-release" {
  inherits   = ["tools"]
  tags       = ["${REGISTRY}/bookstore/tools:${TAG}"]
  cache-from = ["type=registry,ref=${REGISTRY}/bookstore/tools:buildcache"]
  cache-to   = ["type=registry,ref=${REGISTRY}/bookstore/tools:buildcache,mode=max"]
  output     = ["type=registry"]
}
