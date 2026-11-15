#!/usr/bin/env bash
#
# bookstore-deploy.sh - deploys a Bookstore API release on this server.
#
# Run with --help for options.
# Exit codes: 0 success, 1 deploy failed, 2 usage error

set -Eeuo pipefail
shopt -s inherit_errexit

SCRIPT_DIR=$(dirname "$(readlink -f "$0")")
# shellcheck source=lib/common.sh
source "$SCRIPT_DIR/lib/common.sh"

ROOT="${BOOKSTORE_ROOT:-/opt/bookstore}"
ARTIFACT_DIR="${ARTIFACT_DIR:-/var/tmp/bookstore}"
DEPLOY_LOG="${DEPLOY_LOG:-/var/log/bookstore/deploy.log}"
SERVICE="bookstore-api"

# Set by parse_args
ENV_NAME=""
VERSION=""
DRY_RUN=0

# Release folder that is being prepared; cleanup removes it if the deploy stops early.
INCOMING=""

usage() {
    cat <<EOF_USAGE
Usage: ${0##*/} -e ENV -v VERSION [-n] [-h]

Deploys a Bookstore API release on this server.

Options:
  -e, --env ENV          target environment: staging or production
  -v, --version VERSION  release to deploy, for example 1.4.2
  -n, --dry-run          print the commands instead of running them
  -h, --help             show this help and exit

Environment:
  BOOKSTORE_ROOT  installation root (default /opt/bookstore)
  ARTIFACT_DIR    folder with bookstore-api-VERSION-linux-x64.tar.gz
                  (default /var/tmp/bookstore)
  DEPLOY_LOG      messages are appended here as well
                  (default /var/log/bookstore/deploy.log)

Example:
  ${0##*/} -e staging -v 1.4.2 --dry-run
EOF_USAGE
}

usage_error() {
    echo "${0##*/}: $1" >&2
    echo "Try '${0##*/} --help' for more information." >&2
    exit 2
}

parse_args() {
    while (( $# > 0 )); do
        case $1 in
            -e | --env)
                (( $# >= 2 )) || usage_error "option $1 needs a value"
                ENV_NAME=$2
                shift 2
                ;;
            --env=*)
                ENV_NAME=${1#*=}
                shift
                ;;
            -v | --version)
                (( $# >= 2 )) || usage_error "option $1 needs a value"
                VERSION=$2
                shift 2
                ;;
            --version=*)
                VERSION=${1#*=}
                shift
                ;;
            -n | --dry-run)
                DRY_RUN=1
                shift
                ;;
            -h | --help)
                usage
                exit 0
                ;;
            --)
                shift
                break
                ;;
            -*)
                usage_error "unknown option: $1"
                ;;
            *)
                usage_error "unexpected argument: $1"
                ;;
        esac
    done
    (( $# == 0 )) || usage_error "unexpected argument: $1"
}

validate_args() {
    case $ENV_NAME in
        staging | production) ;;
        "") usage_error "missing -e ENV" ;;
        *) usage_error "unknown environment: $ENV_NAME (use staging or production)" ;;
    esac
    [[ -n $VERSION ]] || usage_error "missing -v VERSION"
    [[ $VERSION =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || usage_error "VERSION must look like 1.4.2, got: $VERSION"
}

# Prints the content of the RELEASE file that is written into each release.
release_info() {
    cat <<EOF_INFO
version=$VERSION
environment=$ENV_NAME
deployed_at=$(date -u +%Y-%m-%dT%H:%M:%SZ)
deployed_by=${SUDO_USER:-$(id -un)}
EOF_INFO
}

# ShellCheck 0.11 does not see the call from the trap when main is followed by "; exit".
# shellcheck disable=SC2329
cleanup() {
    if [[ -n $INCOMING && -d $INCOMING ]]; then
        log "removing unfinished release folder $INCOMING"
        rm -rf -- "$INCOMING"
    fi
}

deploy() {
    local artifact="$ARTIFACT_DIR/bookstore-api-$VERSION-linux-x64.tar.gz"
    local release_dir="$ROOT/releases/$VERSION"
    local current
    current=$(current_release "$ROOT") || current=""

    # Running the same deploy twice is not an error: the second run has nothing to do.
    if [[ $current == "$VERSION" ]]; then
        log "release $VERSION is already current, nothing to do"
        return 0
    fi
    [[ ! -e $release_dir ]] || die "release $VERSION already exists but is not current: $release_dir"
    [[ -f $artifact ]] || die "artifact not found: $artifact"

    log "deploying $VERSION to $ENV_NAME"
    # Leftovers of a deploy that was killed with SIGKILL, where no trap can run.
    local stale
    for stale in "$ROOT"/releases/.incoming-*; do
        [[ -d $stale ]] || continue
        log "removing leftover $stale"
        run rm -rf -- "$stale"
    done
    # Extract into a hidden folder first; releases/VERSION appears only when it is complete.
    if (( DRY_RUN )); then
        INCOMING="$ROOT/releases/.incoming-$VERSION.XXXXXX"
    else
        INCOMING=$(mktemp -d "$ROOT/releases/.incoming-$VERSION.XXXXXX")
    fi
    run tar -xzf "$artifact" -C "$INCOMING"
    if (( DRY_RUN )); then
        printf '[dry-run] write %s\n' "$INCOMING/RELEASE"
    else
        release_info > "$INCOMING/RELEASE"
    fi
    run mv -T "$INCOMING" "$release_dir"
    INCOMING=""

    run ln -sfn "$release_dir" "$ROOT/current"
    run sudo systemctl restart "$SERVICE"
    run retry 5 1 "$SCRIPT_DIR/bookstore-health.sh" local || die "health check failed after deploying $VERSION"
    if (( DRY_RUN )); then
        log "dry run finished, nothing was changed"
    else
        log "deployed $VERSION"
    fi
}

main() {
    trap cleanup EXIT
    trap 'exit 130' INT
    trap 'exit 143' TERM
    trap 'on_error "$?" "$LINENO" "$BASH_COMMAND"' ERR

    parse_args "$@"
    validate_args
    if (( ! DRY_RUN )); then
        start_log "$DEPLOY_LOG"
        acquire_lock "$ROOT/.deploy.lock"
    fi
    deploy
}

main "$@"; exit
