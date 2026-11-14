# Uses the real Docker provider: the apply run creates and removes a container
provider "docker" {}

variables {
  name          = "tfseries-test-api"
  image         = "bookstore-api:1.1.0"
  networks      = ["tfseries-shared"]
  data_volumes  = ["covers"]
  external_port = 18482
}

run "rejects_latest_tag" {
  command = plan

  variables {
    image = "bookstore-api:latest"
  }

  expect_failures = [var.image]
}

run "starts_container_with_volume" {
  assert {
    condition     = output.volume_names["covers"] == "tfseries-test-api-covers"
    error_message = "Volume names must be <name>-<data set>."
  }

  assert {
    condition     = docker_container.this.restart == "unless-stopped"
    error_message = "The default restart policy must be unless-stopped."
  }
}
