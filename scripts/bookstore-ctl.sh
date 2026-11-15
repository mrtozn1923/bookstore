#!/usr/bin/env bash
#
# bookstore-ctl.sh - single entry point for everyday tasks on a Bookstore server.
#
# Usage:      bookstore-ctl.sh COMMAND [ARGS...]
# Commands:   status              current release and health check
#             releases            list releases, newest last
#             prune [-n] [KEEP]   delete old releases (see bookstore-prune-releases.sh)
# Variables:  BOOKSTORE_ROOT  installation root (default /opt/bookstore)
# Exit codes: 0 success, 1 error, 2 usage error

SCRIPT_DIR=$(dirname "$(readlink -f "$0")")
ROOT="${BOOKSTORE_ROOT:-/opt/bookstore}"

usage() {
    echo "Usage: ${0##*/} status|releases|prune [ARGS...]" >&2
}

cmd_status() {
    local current
    current=$(readlink -f "$ROOT/current")
    if [[ -z $current || ! -d $current ]]; then
        echo "No current release under $ROOT" >&2
        return 1
    fi
    echo "current release: ${current##*/}"
    "$SCRIPT_DIR/bookstore-health.sh" "$@"
}

cmd_releases() {
    local current name
    current=$(readlink -f "$ROOT/current")
    shopt -s nullglob
    for name in "$ROOT"/releases/*/; do
        name=${name%/}
        echo "${name##*/}"
    done | sort -V | while IFS= read -r name; do
        if [[ $ROOT/releases/$name == "$current" ]]; then
            echo "* $name"
        else
            echo "  $name"
        fi
    done
}

main() {
    local command="${1:-}"
    if (( $# > 0 )); then
        shift
    fi
    case "$command" in
        status)
            cmd_status "$@"
            ;;
        releases)
            cmd_releases
            ;;
        prune)
            "$SCRIPT_DIR/bookstore-prune-releases.sh" "$@"
            ;;
        -h | --help | help)
            usage
            ;;
        "")
            usage
            exit 2
            ;;
        *)
            echo "Unknown command: $command" >&2
            usage
            exit 2
            ;;
    esac
}

main "$@"; exit
