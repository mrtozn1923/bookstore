#!/usr/bin/env bash
#
# bookstore-backup.sh - full backup of the Bookstore SQL Server database, with
# verification, retention and a copy to a second location.
#
# Usage:      bookstore-backup.sh [-n]
#             -n  dry run: print the commands instead of running them
# Files:      /etc/bookstore/backup.env          SQL_SERVER, DB_NAME, BACKUP_DIR, REMOTE_DIR,
#                                                RETENTION_DAYS (default 7), LOCK_FILE
#             /etc/bookstore/backup-secrets.env  SQLCMDUSER, SQLCMDPASSWORD (mode 600)
# Exit codes: 0 success, 1 error, 2 usage error

if ((BASH_VERSINFO[0] < 5)); then
    echo "${0##*/}: needs Bash 5 or newer, this is $BASH_VERSION" >&2
    exit 1
fi
set -Eeuo pipefail
shopt -s inherit_errexit

SCRIPT_DIR=$(dirname "$(readlink -f "$0")")
# shellcheck source=lib/common.sh
source "$SCRIPT_DIR/lib/common.sh"

BACKUP_ENV_FILE="${BACKUP_ENV_FILE:-/etc/bookstore/backup.env}"
BACKUP_SECRETS_FILE="${BACKUP_SECRETS_FILE:-/etc/bookstore/backup-secrets.env}"
DRY_RUN=0

usage() {
    echo "Usage: ${0##*/} [-n]" >&2
}

# Runs one T-SQL statement; -b makes sqlcmd exit with an error code when the statement fails.
# sqlcmd reads the login from SQLCMDUSER and SQLCMDPASSWORD, so no secret is on the command line.
sql() {
    run sqlcmd -S "$SQL_SERVER" -d master -b -Q "$1"
}

backup() {
    local stamp file old
    stamp=$(date -u +%Y%m%dT%H%M%SZ)
    file="$BACKUP_DIR/${DB_NAME}_$stamp.bak"

    log "backing up $DB_NAME to $file"
    # The file is written by SQL Server itself, so BACKUP_DIR is a path on the database server.
    sql "BACKUP DATABASE [$DB_NAME] TO DISK = N'$file' WITH COMPRESSION, CHECKSUM, INIT"
    sql "RESTORE VERIFYONLY FROM DISK = N'$file' WITH CHECKSUM"

    log "copying $file to $REMOTE_DIR"
    run retry 3 5 rsync -a --partial "$file" "$REMOTE_DIR/"

    # Retention: delete local backups older than RETENTION_DAYS.
    while IFS= read -r -d '' old; do
        if ((DRY_RUN)); then
            log "would delete $old"
        else
            rm -f -- "$old"
            log "deleted $old"
        fi
    done < <(find "$BACKUP_DIR" -maxdepth 1 -type f -name "${DB_NAME}_*.bak" -mtime +"$RETENTION_DAYS" -print0)
    log "backup finished: $file"
}

main() {
    trap 'on_error "$?" "$LINENO" "$BASH_COMMAND"' ERR
    case ${1:-} in
        "") ;;
        -n | --dry-run) DRY_RUN=1 ;;
        -h | --help)
            usage
            exit 0
            ;;
        *)
            usage
            exit 2
            ;;
    esac

    load_env_file "$BACKUP_ENV_FILE"
    load_env_file "$BACKUP_SECRETS_FILE" private
    : "${SQL_SERVER:?}" "${BACKUP_DIR:?}" "${REMOTE_DIR:?}" "${SQLCMDUSER:?}" "${SQLCMDPASSWORD:?}"
    RETENTION_DAYS=${RETENTION_DAYS:-7}
    # DB_NAME goes into T-SQL, so only allow plain names.
    [[ ${DB_NAME:-} =~ ^[A-Za-z0-9_]+$ ]] || die "DB_NAME must be letters, digits or _, got: ${DB_NAME:-}"
    [[ $RETENTION_DAYS =~ ^[0-9]+$ ]] || die "RETENTION_DAYS must be a number, got: $RETENTION_DAYS"
    [[ -d $BACKUP_DIR ]] || die "backup folder not found: $BACKUP_DIR"

    acquire_lock "${LOCK_FILE:-/run/lock/bookstore-backup.lock}"
    backup
}

{
    main "$@"
    exit
}
