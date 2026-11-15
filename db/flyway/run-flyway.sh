#!/bin/bash
# Flyway OSS Edition 13.12.2 (Apache 2.0). Needs FLYWAY_PASSWORD and an existing database sqls_24_flyway.
set -euo pipefail
docker run --rm -e FLYWAY_PASSWORD -v "$PWD:/flyway/project" -w /flyway/project flyway/flyway:13.12.2 "${1:-migrate}"
