#!/usr/bin/env bash
set -euo pipefail

docker network create tfseries-dev
docker run -d --name tfseries-dev-api --network tfseries-dev \
    -p 18480:8080 -e Bookstore__Environment=dev bookstore-api:1.0.0
