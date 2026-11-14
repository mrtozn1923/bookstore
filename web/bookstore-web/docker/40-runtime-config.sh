#!/bin/sh
set -eu

# Eksik değişkenle container hiç açılmasın (fail fast)
: "${APP_ENVIRONMENT:?APP_ENVIRONMENT is required}"
: "${API_BASE_URL:?API_BASE_URL is required}"
: "${FREE_SHIPPING_THRESHOLD:?FREE_SHIPPING_THRESHOLD is required}"

envsubst '${APP_ENVIRONMENT} ${API_BASE_URL} ${FREE_SHIPPING_THRESHOLD}' \
  < /etc/bookstore/config.template.json \
  > /usr/share/nginx/html/config.json

echo "runtime config written for ${APP_ENVIRONMENT}"
