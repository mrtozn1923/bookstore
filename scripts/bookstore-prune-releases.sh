#!/usr/bin/env bash
#
# bookstore-prune-releases.sh - deletes old Bookstore releases, keeping the newest ones.
#
# Usage:      bookstore-prune-releases.sh [-n] [KEEP]
#             -n    dry run: only print what would be deleted
#             KEEP  number of releases to keep (default 5)
# Variables:  BOOKSTORE_ROOT  installation root (default /opt/bookstore)
# Exit codes: 0 success, 1 error, 2 usage error

usage() {
    echo "Usage: ${0##*/} [-n] [KEEP]" >&2
}

main() {
    local dry_run=0
    if [[ ${1:-} == "-n" ]]; then
        dry_run=1
        shift
    fi
    if (( $# > 1 )); then
        usage
        exit 2
    fi
    local keep="${1:-5}"
    if [[ ! $keep =~ ^[1-9][0-9]*$ ]]; then
        echo "KEEP must be a positive integer, got: $keep" >&2
        exit 2
    fi

    local root="${BOOKSTORE_ROOT:-/opt/bookstore}"
    local releases_dir="$root/releases"
    if [[ ! -d $releases_dir ]]; then
        echo "Releases directory not found: $releases_dir" >&2
        exit 1
    fi
    local current
    current=$(readlink -f "$root/current")

    shopt -s nullglob
    local dirs=("$releases_dir"/*/)
    if (( ${#dirs[@]} <= keep )); then
        echo "Nothing to prune: ${#dirs[@]} releases, keeping $keep"
        return 0
    fi

    local name dir
    # Newest first with a version sort, so 2026-11-14-10 comes after 2026-11-14-9.
    while IFS= read -r name; do
        dir="$releases_dir/$name"
        if [[ $dir == "$current" ]]; then
            echo "skip $name (current)"
            continue
        fi
        if (( dry_run )); then
            echo "would delete $name"
        else
            rm -rf -- "$dir"
            echo "deleted $name"
        fi
    done < <(for dir in "${dirs[@]}"; do dir=${dir%/}; echo "${dir##*/}"; done | sort -V -r | tail -n +"$((keep + 1))")
}

main "$@"; exit
