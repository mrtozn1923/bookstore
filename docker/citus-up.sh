#!/bin/sh
# Citus 14.2 for post 09: one coordinator (also holds shards) and one worker.
# Set POSTGRES_PASSWORD in your shell first; run from the snapshot root.
set -e
docker network create bookstore-citus
docker run -d --name bookstore-citus-w1 --network bookstore-citus --hostname citus-w1 -e POSTGRES_HOST_AUTH_METHOD=trust citusdata/citus:14.2.0
docker run -d --name bookstore-citus-c --network bookstore-citus --hostname citus-c -p 127.0.0.1:5450:5432 -e POSTGRES_PASSWORD="$POSTGRES_PASSWORD" citusdata/citus:14.2.0
sleep 20
docker exec -u postgres bookstore-citus-c bash -c "umask 077; echo '*:*:*:postgres:$POSTGRES_PASSWORD' > ~/.pgpass"
docker exec bookstore-citus-c psql -U postgres -c "SELECT citus_set_coordinator_host('citus-c')" \
  -c "SELECT citus_add_node('citus-w1', 5432)" -c "SELECT citus_set_node_property('citus-c', 5432, 'shouldhaveshards', true)"
docker exec -i bookstore-citus-c psql -U postgres < db/citus-schema.sql
docker exec -i bookstore-citus-c psql -U postgres < db/citus-data.sql
