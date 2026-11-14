#!/bin/sh
# Restore drill: take a base backup from the current primary into a fresh container and start it standalone.
# Usage: restore-drill.sh <primary-host> <network> <restore-container>
set -e
start=$(date +%s)
docker run -d --name "$3" --network "$2" -e PGPASSWORD="$REPLICATOR_PASSWORD" --entrypoint bash postgres:18 -c '
    set -e
    mkdir -p "$PGDATA"; chown postgres "$PGDATA"; chmod 700 "$PGDATA"
    gosu postgres pg_basebackup -h '"$1"' -U replicator -D "$PGDATA" -X stream -c fast
    exec gosu postgres postgres -c max_connections=200' >/dev/null
until docker exec "$3" pg_isready -U postgres -q 2>/dev/null; do sleep 1; done
echo "restored and accepting connections in $(( $(date +%s) - start )) s"
