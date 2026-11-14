#!/usr/bin/env bash
# CI step: start the stack with compose.ci.yaml, wait until healthy, run the smoke test; the exit code fails the job
set -euo pipefail
export COMPOSE_PROJECT_NAME=composeseries-ci
export COMPOSE_FILE=compose.yaml:compose.ci.yaml
# Always clean up, also when a step fails: the database lives on tmpfs, nothing is kept
trap 'docker compose down -v' EXIT
docker compose up -d --wait
docker compose run --rm smoke
