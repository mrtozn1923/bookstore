#!/usr/bin/env bats
# Tests for scripts/lib/common.sh

bats_require_minimum_version 1.5.0

setup() {
    load test_helper
    STUBS="$BATS_TEST_TMPDIR/stubs"
    mkdir -p "$STUBS"
    PATH="$STUBS:$PATH"
}

# Calls a library function in a child bash. Sourcing the library into the test itself
# would replace bats' own run() with the library's run().
lib() {
    bash -c 'set -Eeuo pipefail; source "$1"; shift; "$@"' lib "$SCRIPTS/lib/common.sh" "$@"
}

@test "log writes a UTC timestamp and the message to stderr only" {
    run --separate-stderr lib log "deploying 1.4.7"
    [ "$status" -eq 0 ]
    [ "$output" = "" ]
    [[ $stderr =~ ^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}Z\ deploying\ 1\.4\.7$ ]] || false
}

@test "die exits with the given code" {
    run lib die "artifact not found" 3
    [ "$status" -eq 3 ]
    [[ $output == *"ERROR: artifact not found" ]] || false
}

@test "run prints a shell-quoted command in a dry run" {
    DRY_RUN=1 run lib run rm -rf "/opt/bookstore/releases/1.4.0 old"
    [ "$status" -eq 0 ]
    [ "$output" = '[dry-run] rm -rf /opt/bookstore/releases/1.4.0\ old' ]
}

@test "retry succeeds on the third attempt" {
    make_stub sleep 'exit 0'
    make_stub flaky "n=\$(( \$(cat $BATS_TEST_TMPDIR/count) + 1 )); echo \$n > $BATS_TEST_TMPDIR/count; (( n >= 3 ))"
    echo 0 >"$BATS_TEST_TMPDIR/count"
    run lib retry 5 1 flaky
    [ "$status" -eq 0 ]
    [ "$(cat "$BATS_TEST_TMPDIR/count")" -eq 3 ]
}

@test "retry gives up and returns the last exit code" {
    make_stub sleep 'exit 0'
    run lib retry 2 1 bash -c 'exit 7'
    [ "$status" -eq 7 ]
    [[ $output == *"giving up after 2 attempts (exit 7)"* ]] || false
}

@test "current_release prints the release that current points to" {
    mkdir -p "$BATS_TEST_TMPDIR/root/releases/1.4.7"
    ln -s "$BATS_TEST_TMPDIR/root/releases/1.4.7" "$BATS_TEST_TMPDIR/root/current"
    run lib current_release "$BATS_TEST_TMPDIR/root"
    [ "$status" -eq 0 ]
    [ "$output" = "1.4.7" ]
}

@test "current_release fails when there is no current release" {
    run lib current_release "$BATS_TEST_TMPDIR/empty"
    [ "$status" -eq 1 ]
}

# shellcheck disable=SC2016  # the child bash expands these
@test "load_env_file exports KEY=VALUE lines and skips comments" {
    printf '# comment\n\nKEEP_RELEASES=3\nARTIFACT_BASE_URL=https://artifacts.bookstore.test/a=b\n' >"$BATS_TEST_TMPDIR/deploy.env"
    chmod 644 "$BATS_TEST_TMPDIR/deploy.env"
    run lib bash -c 'source "$1"; load_env_file "$2"; bash -c '\''echo "$KEEP_RELEASES $ARTIFACT_BASE_URL"'\''' \
        _ "$SCRIPTS/lib/common.sh" "$BATS_TEST_TMPDIR/deploy.env"
    [ "$status" -eq 0 ]
    [ "$output" = "3 https://artifacts.bookstore.test/a=b" ]
}

@test "load_env_file refuses a secrets file that others can read" {
    printf 'SQLCMDPASSWORD=example-password-change-me\n' >"$BATS_TEST_TMPDIR/secrets.env"
    chmod 644 "$BATS_TEST_TMPDIR/secrets.env"
    run lib load_env_file "$BATS_TEST_TMPDIR/secrets.env" private
    [ "$status" -eq 1 ]
    [[ $output == *"must be mode 600 or 400, not 644"* ]] || false
}

@test "load_env_file reports a bad line by number, not by content" {
    printf 'SQLCMDPASSWORD example-password-change-me\n' >"$BATS_TEST_TMPDIR/secrets.env"
    chmod 600 "$BATS_TEST_TMPDIR/secrets.env"
    run lib load_env_file "$BATS_TEST_TMPDIR/secrets.env" private
    [ "$status" -eq 1 ]
    [[ $output == *"secrets.env:1: not a KEY=VALUE line"* ]] || false
    [[ $output != *"example-password"* ]] || false
}
