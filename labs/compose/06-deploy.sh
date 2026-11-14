#!/usr/bin/env bash
# Runs on the server, in the folder with compose.yaml, compose.db.yaml, compose.prod.yaml, config/, db/, secrets/ and .env.
# usage: ./06-deploy.sh 1.3.1   (rollback: the same command with the previous tag)
set -euo pipefail
tag="${1:?usage: $0 <tag>}"
sed -i "s/^TAG=.*/TAG=${tag}/" .env
docker compose pull
docker compose up -d --wait
docker compose ps --format "table {{.Service}}\t{{.Image}}\t{{.Status}}"
