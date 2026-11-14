#!/bin/sh
# PostgreSQL 18 primary (pg1) and streaming replica (pg2) used in posts 01-05.
# Set POSTGRES_PASSWORD and REPLICATOR_PASSWORD in your shell first.
set -e
docker network create bookstore-net
docker run -d --name bookstore-pg1 --network bookstore-net --hostname pg1 -p 5432:5432 \
  -e POSTGRES_PASSWORD="$POSTGRES_PASSWORD" -e POSTGRES_DB=bookstore postgres:18 \
  -c shared_preload_libraries=pg_stat_statements -c max_connections=200 -c wal_level=logical
sleep 5
docker exec bookstore-pg1 bash -c 'echo "host replication replicator all scram-sha-256" >> $PGDATA/pg_hba.conf'
docker exec -i bookstore-pg1 psql -U postgres -d bookstore -v replicator_password="$REPLICATOR_PASSWORD" < db/pg-replication.sql
docker exec bookstore-pg1 psql -U postgres -c "SELECT pg_reload_conf()"
docker run -d --name bookstore-pg2 --network bookstore-net --hostname pg2 -p 5433:5432 \
  -e PGPASSWORD="$REPLICATOR_PASSWORD" --entrypoint bash postgres:18 -c '
    set -e
    if [ ! -s "$PGDATA/PG_VERSION" ]; then
      mkdir -p "$PGDATA"; chown postgres "$PGDATA"; chmod 700 "$PGDATA"
      gosu postgres pg_basebackup -h pg1 -U replicator -D "$PGDATA" -R -X stream -C -S pg2_slot
      sed -i "s/primary_conninfo = '"'"'/primary_conninfo = '"'"'application_name=pg2 /" "$PGDATA/postgresql.auto.conf"
    fi
    exec gosu postgres postgres -c max_connections=200 -c hot_standby_feedback=on'
