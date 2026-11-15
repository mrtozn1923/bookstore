#!/usr/bin/env bash
# Lab only: local OCI registry for post 06 on 127.0.0.1:52570 with basic auth.
# The password comes from REGISTRY_PASSWORD (a throwaway lab value); nothing is committed.
set -euo pipefail
: "${REGISTRY_PASSWORD:?set REGISTRY_PASSWORD first}"
dir="$(mktemp -d)"
htpasswd -Bbn helm-ci "$REGISTRY_PASSWORD" > "$dir/htpasswd"
docker run -d --name helmseries-registry -p 127.0.0.1:52570:5000 \
    -v "$dir/htpasswd:/auth/htpasswd:ro" \
    -e REGISTRY_AUTH=htpasswd -e REGISTRY_AUTH_HTPASSWD_REALM=helmseries -e REGISTRY_AUTH_HTPASSWD_PATH=/auth/htpasswd \
    registry:3
