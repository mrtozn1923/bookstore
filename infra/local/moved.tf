# The API resources moved into the bookstore-service module (post 8 covers moved blocks in detail)
moved {
  from = docker_container.api
  to   = module.api.docker_container.this
}

moved {
  from = docker_volume.api_data
  to   = module.api.docker_volume.data
}

moved {
  from = terraform_data.api_image
  to   = module.api.terraform_data.image
}
