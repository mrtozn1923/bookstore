#!/usr/bin/env bash
#
# bookstore-health.sh - checks the Bookstore API health endpoint.
#
# Usage:      bookstore-health.sh [URL]
# Default:    http://127.0.0.1:5000/health
# Exit codes: 0 healthy, 1 unhealthy or unreachable, 2 usage error

DEFAULT_URL="http://127.0.0.1:5000/health"

usage() {
    echo "Usage: ${0##*/} [URL]" >&2
}

main() {
    if [ "$#" -gt 1 ]; then
        usage
        exit 2
    fi
    local url="${1:-$DEFAULT_URL}"

    if curl -fsS --max-time 5 -o /dev/null "$url"; then
        echo "OK $url"
    else
        echo "FAIL $url" >&2
        exit 1
    fi
}

main "$@"; exit
