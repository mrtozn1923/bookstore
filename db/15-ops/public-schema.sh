#!/bin/bash
# Post 15 - does pg_upgrade keep PostgreSQL 14's CREATE privilege on the public schema?
#   docker build -t bookstore/pg14to18 db/15-ops
#   docker run -d --name bookstore-pg14 --entrypoint sleep -v "${PWD}\db:/lab:ro" bookstore/pg14to18 infinity
#   docker exec -u postgres -w /tmp bookstore-pg14 bash /lab/15-ops/public-schema.sh
set -e
B14=/usr/lib/postgresql/14/bin; B18=/usr/lib/postgresql/18/bin
OLD=/var/lib/postgresql/14/data; NEW=/var/lib/postgresql/18/data
rm -rf $OLD $NEW; mkdir -p $(dirname $OLD) $(dirname $NEW)
$B14/initdb -D $OLD -U postgres --auth=trust >/dev/null
$B14/pg_ctl -D $OLD -o "-p 5414" -l /tmp/pg14.log start -w >/dev/null
psql -X -p 5414 -q -c "CREATE ROLE bookstore_app LOGIN" -c "CREATE DATABASE bookstore"
echo "14, public schema ACL: $(psql -X -p 5414 -d bookstore -tAc "SELECT nspacl FROM pg_namespace WHERE nspname = 'public'")"
$B14/pg_ctl -D $OLD stop -m fast >/dev/null
$B18/initdb -D $NEW -U postgres --auth=trust --no-data-checksums >/dev/null
$B18/pg_upgrade -b $B14 -B $B18 -d $OLD -D $NEW >/tmp/upgrade14.log 2>&1
$B18/pg_ctl -D $NEW -o "-p 5418" -l /tmp/pg18.log start -w >/dev/null
echo "18 after pg_upgrade: $(psql -X -p 5418 -d bookstore -tAc "SELECT nspacl FROM pg_namespace WHERE nspname = 'public'")"
psql -X -p 5418 -q -c "CREATE DATABASE created_after_upgrade"
for db in bookstore created_after_upgrade; do
    echo -n "$db: "; psql -X -p 5418 -d $db -U bookstore_app -c "CREATE TABLE public.app_scratch (id int)" 2>&1
done
$B18/pg_ctl -D $NEW stop -m fast >/dev/null
