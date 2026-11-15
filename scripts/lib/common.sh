# shellcheck shell=bash
#
# common.sh - shared functions for the Bookstore ops scripts.
# Source it from a script; do not run it directly.

if [[ ${BASH_SOURCE[0]} == "$0" ]]; then
    echo "${0##*/} is a library; source it from a script instead of running it" >&2
    exit 2
fi

# Writes a timestamped message to stderr, so stdout stays free for data.
log() {
    printf '%s %s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$*" >&2
}

# Logs an error and exits. Usage: die MESSAGE [EXIT_CODE]
die() {
    log "ERROR: $1"
    exit "${2:-1}"
}

# Runs a command, or only prints it (shell-quoted) when DRY_RUN is 1.
run() {
    if (( ${DRY_RUN:-0} )); then
        printf '[dry-run]'
        printf ' %q' "$@"
        printf '\n'
        return 0
    fi
    "$@"
}

# Prints the name of the release that ROOT/current points to.
# Usage: current_release [ROOT]; returns 1 when there is no current release.
current_release() {
    local root="${1:-${BOOKSTORE_ROOT:-/opt/bookstore}}"
    local target
    target=$(readlink -f "$root/current") || return 1
    [[ -d $target ]] || return 1
    echo "${target##*/}"
}
