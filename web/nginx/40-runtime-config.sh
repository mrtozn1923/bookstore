#!/bin/sh
# Writes config.json from environment variables when the container starts
set -eu

: "${API_BASE_URL:?API_BASE_URL must be set}"
export APP_ENVIRONMENT="${APP_ENVIRONMENT:-production}"

envsubst '${API_BASE_URL} ${APP_ENVIRONMENT}' \
    < /etc/bookstore/config.template.json \
    > /usr/share/nginx/html/config.json

echo "$0: config.json written for $APP_ENVIRONMENT, API at $API_BASE_URL"
