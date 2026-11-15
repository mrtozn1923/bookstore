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

# Copies everything written to stderr from now on into FILE as well.
# stdout is left alone, so it stays clean for data. Usage: start_log FILE
start_log() {
    local file=$1
    mkdir -p "$(dirname "$file")" || return 1
    # tee ignores INT and TERM: a Ctrl-C to the whole process group must not stop
    # the log while the script's traps are still writing to it.
    exec 2> >(trap '' INT TERM; exec tee -a "$file" >&2)
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

# ERR trap handler: logs the failed command, its exit code and the call stack.
# Usage: trap 'on_error "$?" "$LINENO" "$BASH_COMMAND"' ERR   (with set -E)
on_error() {
    local status=$1 line=$2 command=$3 i
    log "ERROR: '$command' exited with $status"
    # FUNCNAME[0] is on_error itself; the last entry is the script's top level.
    for (( i = 1; i < ${#FUNCNAME[@]} - 1; i++ )); do
        log "  at ${FUNCNAME[i]} (${BASH_SOURCE[i]##*/}:$line)"
        line=${BASH_LINENO[i]}
    done
}

# Runs a command until it succeeds, at most ATTEMPTS times; the delay doubles after each failure.
# Usage: retry ATTEMPTS DELAY_SECONDS COMMAND [ARGS...]; returns the last exit code.
retry() {
    local attempts=$1 delay=$2 attempt=1 status
    shift 2
    while true; do
        "$@" && return 0
        status=$?
        if (( attempt >= attempts )); then
            log "giving up after $attempt attempts (exit $status): $*"
            return "$status"
        fi
        log "attempt $attempt failed (exit $status), retrying in ${delay}s: $*"
        sleep "$delay"
        attempt=$((attempt + 1))
        delay=$((delay * 2))
    done
}

# Takes an exclusive lock that is held until the script exits.
# Fails at once if another process holds it. Usage: acquire_lock FILE
acquire_lock() {
    exec 9> "$1"
    flock -n 9 || die "another run holds the lock $1"
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
