#!/usr/bin/env bats
# Tests for scripts/bookstore-deploy.sh against a fake server in BATS_TEST_TMPDIR

setup() {
    load test_helper
    setup_server
}

current() {
    basename "$(readlink "$BOOKSTORE_ROOT/current")"
}

@test "--help prints the usage and exits 0" {
    run "$SCRIPTS/bookstore-deploy.sh" --help
    [ "$status" -eq 0 ]
    [[ ${lines[0]} == "Usage: bookstore-deploy.sh -e ENV -v VERSION [-n] [-h]" ]] || false
}

@test "an unknown environment is a usage error" {
    run "$SCRIPTS/bookstore-deploy.sh" -e qa -v 1.4.7
    [ "$status" -eq 2 ]
    [[ ${lines[0]} == *"unknown environment: qa"* ]] || false
}

@test "a version that is not X.Y.Z is a usage error" {
    run "$SCRIPTS/bookstore-deploy.sh" -e staging -v 1.4
    [ "$status" -eq 2 ]
}

@test "a dry run changes nothing" {
    publish_artifact 1.4.7
    run "$SCRIPTS/bookstore-deploy.sh" -e staging -v 1.4.7 --dry-run
    [ "$status" -eq 0 ]
    [ "$(current)" = 1.4.6 ]
    [ ! -e "$BOOKSTORE_ROOT/releases/1.4.7" ]
}

@test "a deploy switches current and writes RELEASE" {
    publish_artifact 1.4.7
    run "$SCRIPTS/bookstore-deploy.sh" -e staging -v 1.4.7
    [ "$status" -eq 0 ]
    [ "$(current)" = 1.4.7 ]
    grep -q '^version=1.4.7$' "$BOOKSTORE_ROOT/releases/1.4.7/RELEASE"
    grep -q 'systemctl restart bookstore-api' "$BATS_TEST_TMPDIR/systemctl.calls"
}

@test "deploying the current release again does nothing" {
    publish_artifact 1.4.7
    "$SCRIPTS/bookstore-deploy.sh" -e staging -v 1.4.7 2>/dev/null
    run "$SCRIPTS/bookstore-deploy.sh" -e staging -v 1.4.7
    [ "$status" -eq 0 ]
    [[ $output == *"already current, nothing to do"* ]] || false
}

@test "a checksum mismatch stops before anything is extracted" {
    publish_artifact 1.4.7
    echo "0000  bookstore-api-1.4.7-linux-x64.tar.gz" >"$STORE/bookstore-api-1.4.7-linux-x64.tar.gz.sha256"
    run "$SCRIPTS/bookstore-deploy.sh" -e staging -v 1.4.7
    [ "$status" -eq 1 ]
    [[ $output == *"checksum mismatch"* ]] || false
    [ "$(current)" = 1.4.6 ]
    [ ! -e "$BOOKSTORE_ROOT/releases/1.4.7" ]
}

@test "a failed health check rolls back to the previous release" {
    publish_artifact 1.4.7 unhealthy
    run "$SCRIPTS/bookstore-deploy.sh" -e staging -v 1.4.7
    [ "$status" -eq 1 ]
    [[ $output == *"rolled back to 1.4.6"* ]] || false
    [ "$(current)" = 1.4.6 ]
}

@test "--rollback returns to the release before current" {
    run "$SCRIPTS/bookstore-deploy.sh" -e staging --rollback
    [ "$status" -eq 0 ]
    [ "$(current)" = 1.4.5 ]
}
