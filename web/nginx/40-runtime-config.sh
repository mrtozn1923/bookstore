#!/bin/sh
# Writes config.json from environment variables when the container starts.
# /tmp is the only writable path, so the image also runs with --read-only --tmpfs /tmp
set -eu

: "${API_BASE_URL:?API_BASE_URL must be set}"
export APP_ENVIRONMENT="${APP_ENVIRONMENT:-production}"

envsubst '${API_BASE_URL} ${APP_ENVIRONMENT}' \
    < /etc/bookstore/config.template.json \
    > /tmp/config.json

echo "$0: config.json written for $APP_ENVIRONMENT, API at $API_BASE_URL"
