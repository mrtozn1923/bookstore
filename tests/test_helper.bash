# shellcheck shell=bash
# Shared helpers for the bats tests: a throwaway server layout and stub commands on PATH.

REPO_ROOT=$(cd "$BATS_TEST_DIRNAME/.." && pwd)
# shellcheck disable=SC2034  # used by the .bats files
SCRIPTS="$REPO_ROOT/scripts"

# Creates an executable stub NAME in $STUBS whose body is the remaining arguments.
make_stub() {
    local name=$1
    shift
    printf '#!/usr/bin/env bash\n%s\n' "$*" >"$STUBS/$name"
    chmod +x "$STUBS/$name"
}

# Builds a fake server under BATS_TEST_TMPDIR: releases 1.4.5 and 1.4.6 (current),
# an artifact store, config files and stubs for sudo, systemctl and the health endpoint.
setup_server() {
    export BOOKSTORE_ROOT="$BATS_TEST_TMPDIR/opt/bookstore"
    export ARTIFACT_DIR="$BATS_TEST_TMPDIR/downloads"
    export DEPLOY_LOG="$BATS_TEST_TMPDIR/deploy.log"
    export DEPLOY_ENV_FILE="$BATS_TEST_TMPDIR/deploy.env"
    export DEPLOY_SECRETS_FILE="$BATS_TEST_TMPDIR/deploy-secrets.env"
    STORE="$BATS_TEST_TMPDIR/store"
    STUBS="$BATS_TEST_TMPDIR/stubs"
    mkdir -p "$BOOKSTORE_ROOT/releases/1.4.5" "$BOOKSTORE_ROOT/releases/1.4.6" "$STORE" "$STUBS"
    ln -s "$BOOKSTORE_ROOT/releases/1.4.6" "$BOOKSTORE_ROOT/current"

    printf 'ARTIFACT_BASE_URL=file://%s\nKEEP_RELEASES=5\nHEALTH_RETRIES=0\n' "$STORE" >"$DEPLOY_ENV_FILE"
    printf 'MIGRATION_CONNECTION=Server=127.0.0.1;Password=example-password-change-me\n' >"$DEPLOY_SECRETS_FILE"
    chmod 644 "$DEPLOY_ENV_FILE"
    chmod 600 "$DEPLOY_SECRETS_FILE"

    # The health endpoint answers 503 while the current release has an UNHEALTHY file;
    # every other curl call (the artifact download) goes to the real curl.
    local real_curl
    real_curl=$(command -v curl)
    make_stub curl "for arg in \"\$@\"; do
    if [[ \$arg == http://127.0.0.1:5000/health ]]; then
        [[ ! -e $BOOKSTORE_ROOT/current/UNHEALTHY ]] && exit 0
        echo 'curl: (22) The requested URL returned error: 503' >&2
        exit 22
    fi
done
exec $real_curl \"\$@\""
    make_stub sudo 'exec "$@"'
    # shellcheck disable=SC2016  # expands when the stub runs
    make_stub systemctl 'echo "systemctl $*" >> "$BATS_TEST_TMPDIR/systemctl.calls"'
    export PATH="$STUBS:$PATH"
}

# Publishes bookstore-api-VERSION-linux-x64.tar.gz and its .sha256 in the artifact store.
# Usage: publish_artifact VERSION [unhealthy]
publish_artifact() {
    local version=$1 dir
    dir=$(mktemp -d "$BATS_TEST_TMPDIR/artifact.XXXXXX")
    echo "$version" >"$dir/VERSION"
    printf '#!/bin/sh\necho "efbundle: ok"\n' >"$dir/efbundle"
    chmod +x "$dir/efbundle"
    if [[ ${2:-} == unhealthy ]]; then
        touch "$dir/UNHEALTHY"
    fi
    tar -czf "$STORE/bookstore-api-$version-linux-x64.tar.gz" -C "$dir" .
    (cd "$STORE" && sha256sum "bookstore-api-$version-linux-x64.tar.gz" >"bookstore-api-$version-linux-x64.tar.gz.sha256")
}
