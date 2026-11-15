#!/usr/bin/env bash
#
# bookstore-health.sh - checks the Bookstore API health endpoint.
#
# Usage:      bookstore-health.sh [ENVIRONMENT|URL]
#             ENVIRONMENT is local (default), staging or production.
# Variables:  HEALTH_TIMEOUT  seconds before curl gives up (default 5)
# Exit codes: 0 healthy, 1 unhealthy or unreachable, 2 usage error

declare -rA HEALTH_URLS=(
    [local]="http://127.0.0.1:5000/health"
    [staging]="https://staging.bookstore.test/health"
    [production]="https://bookstore.test/health"
)

usage() {
    echo "Usage: ${0##*/} [local|staging|production|URL]" >&2
}

main() {
    if [ "$#" -gt 1 ]; then
        usage
        exit 2
    fi
    local target="${1:-local}"
    # A known environment name maps to its URL; anything else is used as a URL.
    local url="${HEALTH_URLS[$target]:-$target}"
    local curl_opts=(-fsS --max-time "${HEALTH_TIMEOUT:-5}" -o /dev/null)

    if curl "${curl_opts[@]}" "$url"; then
        echo "OK $url"
    else
        echo "FAIL $url" >&2
        exit 1
    fi
}

main "$@"; exit
