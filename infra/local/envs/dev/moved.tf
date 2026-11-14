# This environment's resources used to live in the root module (infra/local)
moved {
  from = docker_network.bookstore
  to   = module.bookstore.docker_network.bookstore
}

moved {
  from = module.api
  to   = module.bookstore.module.api
}
