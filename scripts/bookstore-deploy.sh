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
DEPLOY_ENV_FILE="${DEPLOY_ENV_FILE:-/etc/bookstore/deploy.env}"
DEPLOY_SECRETS_FILE="${DEPLOY_SECRETS_FILE:-/etc/bookstore/deploy-secrets.env}"
SERVICE="bookstore-api"

# Set by parse_args
ENV_NAME=""
VERSION=""
DRY_RUN=0
ROLLBACK=0

# Release folder that is being prepared; cleanup removes it if the deploy stops early.
INCOMING=""

usage() {
    cat <<EOF_USAGE
Usage: ${0##*/} -e ENV -v VERSION [-n] [-h]
       ${0##*/} -e ENV --rollback [-n]

Deploys a Bookstore API release on this server, or switches back to the
previous release with --rollback.

Options:
  -e, --env ENV          target environment: staging or production
  -v, --version VERSION  release to deploy, for example 1.4.7
      --rollback         switch current back to the previous release
  -n, --dry-run          print the commands instead of running them
  -h, --help             show this help and exit

Files:
  $DEPLOY_ENV_FILE
      ARTIFACT_BASE_URL (required), KEEP_RELEASES (default 5),
      HEALTH_RETRIES (default 3)
  $DEPLOY_SECRETS_FILE (mode 600)
      MIGRATION_CONNECTION, the connection string for the migration bundle

Environment:
  BOOKSTORE_ROOT  installation root (default /opt/bookstore)
  ARTIFACT_DIR    download folder (default /var/tmp/bookstore)
  DEPLOY_LOG      messages are appended here as well
                  (default /var/log/bookstore/deploy.log)

Example:
  ${0##*/} -e staging -v 1.4.7 --dry-run
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
            --rollback)
                ROLLBACK=1
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
    if (( ROLLBACK )); then
        [[ -z $VERSION ]] || usage_error "--rollback does not take -v"
        return 0
    fi
    [[ -n $VERSION ]] || usage_error "missing -v VERSION"
    [[ $VERSION =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || usage_error "VERSION must look like 1.4.7, got: $VERSION"
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

# Downloads the artifact and its .sha256 file into ARTIFACT_DIR and checks the checksum.
fetch_artifact() {
    local name="bookstore-api-$VERSION-linux-x64.tar.gz"
    local file="$ARTIFACT_DIR/$name"
    local expected actual
    run mkdir -p "$ARTIFACT_DIR"
    run curl -fsSL --retry 3 --retry-connrefused -o "$file.sha256" "$ARTIFACT_BASE_URL/$name.sha256"
    run curl -fsSL --retry 3 --retry-connrefused -o "$file.part" "$ARTIFACT_BASE_URL/$name"
    if (( DRY_RUN )); then
        return 0
    fi
    read -r expected _ < "$file.sha256"
    actual=$(sha256sum "$file.part")
    actual=${actual%% *}
    if [[ $actual != "$expected" ]]; then
        rm -f -- "$file.part"
        die "checksum mismatch for $name: expected $expected, got $actual"
    fi
    mv -f -- "$file.part" "$file"
    log "artifact verified, sha256 $actual"
}

# Points ROOT/current at a release atomically: a new symlink is renamed over the old one.
switch_current() {
    run ln -sfn "$1" "$ROOT/current.new"
    run mv -T "$ROOT/current.new" "$ROOT/current"
}

# Restarts the service and waits until the health check passes.
# It is called inside conditions, where set -e does not apply, so every step is checked.
restart_and_check() {
    run sudo systemctl restart "$SERVICE" || return 1
    HEALTH_RETRIES=$HEALTH_RETRIES run "$SCRIPT_DIR/bookstore-health.sh" local || return 1
}

# Prints the newest release that is older than RELEASE; returns 1 if there is none.
previous_release() {
    local name previous=""
    while IFS= read -r name; do
        [[ $name == "$1" ]] && break
        previous=$name
    done < <(find "$ROOT/releases" -mindepth 1 -maxdepth 1 -type d ! -name '.*' -printf '%f\n' | sort -V)
    [[ -n $previous ]] || return 1
    echo "$previous"
}

deploy() {
    local release_dir="$ROOT/releases/$VERSION"
    local current stale
    current=$(current_release "$ROOT") || current=""

    # Running the same deploy twice is not an error: the second run has nothing to do.
    if [[ $current == "$VERSION" ]]; then
        log "release $VERSION is already current, nothing to do"
        return 0
    fi
    [[ ! -e $release_dir ]] || die "release $VERSION already exists but is not current: $release_dir"

    log "deploying $VERSION to $ENV_NAME (current: ${current:-none})"
    # Leftovers of a deploy that was killed with SIGKILL, where no trap can run.
    for stale in "$ROOT"/releases/.incoming-*; do
        [[ -d $stale ]] || continue
        log "removing leftover $stale"
        run rm -rf -- "$stale"
    done

    fetch_artifact

    # Extract into a hidden folder first; releases/VERSION appears only when it is complete.
    if (( DRY_RUN )); then
        INCOMING="$ROOT/releases/.incoming-$VERSION.XXXXXX"
    else
        INCOMING=$(mktemp -d "$ROOT/releases/.incoming-$VERSION.XXXXXX")
    fi
    run tar -xzf "$ARTIFACT_DIR/bookstore-api-$VERSION-linux-x64.tar.gz" -C "$INCOMING"
    if (( DRY_RUN )); then
        printf '[dry-run] write %s\n' "$INCOMING/RELEASE"
    else
        release_info > "$INCOMING/RELEASE"
    fi

    # The bundle gets the connection string from its environment, not as an argument,
    # so it does not show up in ps. Migrations must keep working with the old release.
    ConnectionStrings__BookstoreDb=$MIGRATION_CONNECTION run "$INCOMING/efbundle"

    run mv -T "$INCOMING" "$release_dir"
    INCOMING=""

    switch_current "$release_dir"
    if ! restart_and_check; then
        [[ -n $current ]] || die "health check failed after deploying $VERSION; no previous release to return to"
        log "health check failed after deploying $VERSION, rolling back to $current"
        switch_current "$ROOT/releases/$current"
        restart_and_check || die "rollback to $current failed too; the service needs attention"
        die "deploy of $VERSION failed; rolled back to $current"
    fi

    if (( DRY_RUN )); then
        run "$SCRIPT_DIR/bookstore-prune-releases.sh" -n "$KEEP_RELEASES"
        log "dry run finished, nothing was changed"
    else
        "$SCRIPT_DIR/bookstore-prune-releases.sh" "$KEEP_RELEASES" >&2
        log "deployed $VERSION"
    fi
}

rollback() {
    local current previous
    current=$(current_release "$ROOT") || die "no current release under $ROOT"
    previous=$(previous_release "$current") || die "no release older than $current to roll back to"
    log "rolling back from $current to $previous"
    switch_current "$ROOT/releases/$previous"
    restart_and_check || die "health check failed after rolling back to $previous"
    if (( DRY_RUN )); then
        log "dry run finished, nothing was changed"
    else
        log "rolled back to $previous"
    fi
}

main() {
    trap cleanup EXIT
    trap 'exit 130' INT
    trap 'exit 143' TERM
    trap 'on_error "$?" "$LINENO" "$BASH_COMMAND"' ERR

    parse_args "$@"
    validate_args
    load_env_file "$DEPLOY_ENV_FILE"
    : "${ARTIFACT_BASE_URL:?must be set in $DEPLOY_ENV_FILE}"
    KEEP_RELEASES=${KEEP_RELEASES:-5}
    HEALTH_RETRIES=${HEALTH_RETRIES:-3}
    if (( ! DRY_RUN )); then
        start_log "$DEPLOY_LOG"
        acquire_lock "$ROOT/.deploy.lock"
    fi
    if (( ROLLBACK )); then
        rollback
        return
    fi
    load_env_file "$DEPLOY_SECRETS_FILE" private
    : "${MIGRATION_CONNECTION:?must be set in $DEPLOY_SECRETS_FILE}"
    deploy
}

main "$@"; exit
