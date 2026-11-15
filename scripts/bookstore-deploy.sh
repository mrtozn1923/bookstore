#!/usr/bin/env bash
#
# bookstore-deploy.sh - deploys a Bookstore API release on this server.
#
# Run with --help for options.
# Exit codes: 0 success, 1 deploy failed, 2 usage error

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

deploy() {
    local artifact="$ARTIFACT_DIR/bookstore-api-$VERSION-linux-x64.tar.gz"
    local release_dir="$ROOT/releases/$VERSION"

    [[ -f $artifact ]] || die "artifact not found: $artifact"
    [[ ! -e $release_dir ]] || die "release $VERSION already exists: $release_dir"

    log "deploying $VERSION to $ENV_NAME"
    run mkdir -p "$release_dir"
    run tar -xzf "$artifact" -C "$release_dir"
    if (( DRY_RUN )); then
        printf '[dry-run] write %s\n' "$release_dir/RELEASE"
    else
        release_info > "$release_dir/RELEASE"
    fi
    run ln -sfn "$release_dir" "$ROOT/current"
    run sudo systemctl restart "$SERVICE"
    run "$SCRIPT_DIR/bookstore-health.sh" local || die "health check failed after deploying $VERSION"
    if (( DRY_RUN )); then
        log "dry run finished, nothing was changed"
    else
        log "deployed $VERSION"
    fi
}

main() {
    parse_args "$@"
    validate_args
    if (( ! DRY_RUN )); then
        start_log "$DEPLOY_LOG"
    fi
    deploy
}

main "$@"; exit
