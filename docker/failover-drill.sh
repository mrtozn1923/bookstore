#!/bin/sh
# Game day: kill the primary 20 s into the drill, promote the replica 6 s later (what Patroni would do).
# Usage: failover-drill.sh <primary-container> <replica-container>
sleep 20
docker kill "$1" >/dev/null && echo "[$(date +%T)] primary killed"
sleep 6
docker exec "$2" psql -U postgres -At -c "SELECT pg_promote()" >/dev/null && echo "[$(date +%T)] replica promoted"
