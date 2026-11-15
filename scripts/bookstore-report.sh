#!/usr/bin/env bash
#
# bookstore-report.sh - summary of the releases on this server and of the deploy log.
#
# Usage:      bookstore-report.sh [--json]
# Variables:  BOOKSTORE_ROOT  installation root (default /opt/bookstore)
#             DEPLOY_LOG      deploy log (default /var/log/bookstore/deploy.log)
# Exit codes: 0 success, 1 error, 2 usage error

SCRIPT_DIR=$(dirname "$(readlink -f "$0")")
# shellcheck source=lib/common.sh
source "$SCRIPT_DIR/lib/common.sh"

ROOT="${BOOKSTORE_ROOT:-/opt/bookstore}"
DEPLOY_LOG="${DEPLOY_LOG:-/var/log/bookstore/deploy.log}"

usage() {
    echo "Usage: ${0##*/} [--json]" >&2
}

# Release names, oldest first.
list_releases() {
    local dir
    shopt -s nullglob
    for dir in "$ROOT"/releases/*/; do
        dir=${dir%/}
        echo "${dir##*/}"
    done | sort -V
}

# One tab-separated row per release: version, deployed_at, deployed_by, current (yes/no).
release_rows() {
    local current name key value deployed_at deployed_by is_current
    current=$(current_release "$ROOT") || current=""
    while IFS= read -r name; do
        deployed_at="-"
        deployed_by="-"
        if [[ -f $ROOT/releases/$name/RELEASE ]]; then
            while IFS='=' read -r key value; do
                case $key in
                    deployed_at) deployed_at=$value ;;
                    deployed_by) deployed_by=$value ;;
                esac
            done < "$ROOT/releases/$name/RELEASE"
        fi
        is_current=no
        if [[ $name == "$current" ]]; then
            is_current=yes
        fi
        printf '%s\t%s\t%s\t%s\n' "$name" "$deployed_at" "$deployed_by" "$is_current"
    done < <(list_releases)
}

# Counts successful and failed deploys in the deploy log.
deploy_counts() {
    local ok=0 failed=0 line
    if [[ ! -r $DEPLOY_LOG ]]; then
        log "no deploy log at $DEPLOY_LOG"
        return 0
    fi
    while IFS= read -r line; do
        case $line in
            *" deployed "*) ok=$((ok + 1)) ;;
            *" ERROR: "*) failed=$((failed + 1)) ;;
        esac
    done < "$DEPLOY_LOG"
    echo "deploys: $ok succeeded, $failed failed (from $DEPLOY_LOG)"
}

print_text() {
    local version deployed_at deployed_by is_current count=0
    printf '%-9s %-21s %-6s %s\n' RELEASE DEPLOYED_AT BY CURRENT
    while IFS=$'\t' read -r version deployed_at deployed_by is_current; do
        printf '%-9s %-21s %-6s %s\n' "$version" "$deployed_at" "$deployed_by" "$is_current"
        count=$((count + 1))
    done < <(release_rows)
    echo "$count releases"
    deploy_counts
}

print_json() {
    release_rows | jq -Rn '[inputs | split("\t") | {
        version: .[0],
        deployed_at: (if .[1] == "-" then null else .[1] end),
        deployed_by: (if .[2] == "-" then null else .[2] end),
        current: (.[3] == "yes")
    }]'
}

main() {
    case ${1:-} in
        "") print_text ;;
        --json) print_json ;;
        -h | --help) usage ;;
        *)
            usage
            exit 2
            ;;
    esac
}

main "$@"; exit
