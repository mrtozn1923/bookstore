#!/usr/bin/env bash
# Docker series 02: install Docker Engine on Ubuntu 24.04 from Docker's official apt repository.
# Run on a fresh Ubuntu 24.04 server as a user with sudo rights.
set -euo pipefail

# 1. Remove distro/unofficial packages that conflict with Docker's packages
sudo apt-get remove -y $(dpkg --get-selections docker.io docker-compose docker-compose-v2 docker-doc docker-buildx podman-docker containerd runc 2>/dev/null | cut -f1) || true

# 2. Docker's GPG key and repository (deb822 format)
sudo apt-get update
sudo apt-get install -y ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc
sudo tee /etc/apt/sources.list.d/docker.sources > /dev/null <<SOURCES
Types: deb
URIs: https://download.docker.com/linux/ubuntu
Suites: $(. /etc/os-release && echo "${UBUNTU_CODENAME:-$VERSION_CODENAME}")
Components: stable
Architectures: $(dpkg --print-architecture)
Signed-By: /etc/apt/keyrings/docker.asc
SOURCES
sudo apt-get update

# 3. Engine, CLI, containerd, Buildx and Compose plugins
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

# 4. Check
sudo docker version
sudo docker run --rm hello-world
