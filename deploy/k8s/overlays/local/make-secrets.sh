#!/usr/bin/env bash
# Creates catalog.env and dependencies.env with random passwords for a local cluster. Both are git-ignored.
set -euo pipefail
cd "$(dirname "$0")"
pw() { openssl rand -base64 32 | tr -dc 'A-Za-z0-9' | cut -c1-24; }
sql="Bs1$(pw)"; redis="$(pw)"; rabbit="$(pw)"
cat > dependencies.env <<ENV
SQL_PASSWORD=$sql
REDIS_PASSWORD=$redis
RABBITMQ_PASSWORD=$rabbit
ENV
cat > catalog.env <<ENV
ConnectionStrings__catalogdb=Server=sql,1433;Database=catalogdb;User ID=sa;Password=$sql;TrustServerCertificate=true
ConnectionStrings__redis=redis:6379,password=$redis
ConnectionStrings__rabbitmq=amqp://bookstore:$rabbit@rabbitmq:5672
ENV
echo "catalog.env and dependencies.env written"
