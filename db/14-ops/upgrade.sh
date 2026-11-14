#!/bin/bash
# Post 14 - pg_upgrade from 17 to 18 (and post 13's missing-library check).
#   docker exec -u postgres -w /tmp bookstore-ops bash /lab/14-ops/upgrade.sh
set -e
B17=/usr/lib/postgresql/17/bin; B18=/usr/lib/postgresql/18/bin
OLD=/var/lib/postgresql/17/data; NEW=/var/lib/postgresql/18/data
rm -rf $OLD $NEW; mkdir -p $(dirname $OLD) $(dirname $NEW)

# old cluster: PostgreSQL 17 (initdb default: no data checksums), Bookstore schema, pg_cron, extended statistics
$B17/initdb -D $OLD -U postgres --auth=trust >/dev/null
$B17/pg_ctl -D $OLD -o "-p 5417 -c shared_preload_libraries=pg_cron -c cron.database_name=bookstore" -l /tmp/pg17.log start -w >/dev/null
psql -X -p 5417 -q -c "CREATE ROLE bookstore_owner LOGIN" -c "CREATE ROLE bookstore_app LOGIN" -c "CREATE DATABASE bookstore OWNER bookstore_owner"
psql -X -p 5417 -q -U bookstore_owner -d bookstore -f /lab/schema.sql >/dev/null
psql -X -p 5417 -q -d bookstore -c "CREATE EXTENSION pg_cron" \
    -c "CREATE STATISTICS st_customers_country_city (dependencies) ON country, city FROM customers" -c "ANALYZE customers"
psql -X -p 5417 -tAc "SHOW data_checksums"
$B17/pg_ctl -D $OLD stop -m fast >/dev/null

check() { $B18/pg_upgrade --check -b $B17 -B $B18 -d $OLD -D $NEW 2>&1 | grep -E "fatal|checksums|could not load|Clusters are compatible" || true; }

echo "== 1. new cluster with the 18 initdb defaults"
$B18/initdb -D $NEW -U postgres --auth=trust | grep -i checksums
check
echo "== 2. --no-data-checksums, pg_cron still installed in the old cluster"
rm -rf $NEW; $B18/initdb -D $NEW -U postgres --auth=trust --no-data-checksums >/dev/null
check
cat $NEW/pg_upgrade_output.d/*/loadable_libraries.txt 2>/dev/null || true
echo "== 3. without pg_cron"
$B17/pg_ctl -D $OLD -o "-p 5417 -c shared_preload_libraries=pg_cron -c cron.database_name=bookstore" -l /tmp/pg17.log start -w >/dev/null
psql -X -p 5417 -q -d bookstore -c "DROP EXTENSION pg_cron"
$B17/pg_ctl -D $OLD stop -m fast >/dev/null
rm -rf $NEW; $B18/initdb -D $NEW -U postgres --auth=trust --no-data-checksums >/dev/null
check

echo "== 4. copy mode vs link mode"
du -sh $OLD/base
TIMEFORMAT="copy: %R s"; time $B18/pg_upgrade -b $B17 -B $B18 -d $OLD -D $NEW >/tmp/upgrade-copy.log 2>&1
rm -rf $NEW; $B18/initdb -D $NEW -U postgres --auth=trust --no-data-checksums >/dev/null
TIMEFORMAT="link: %R s"; time $B18/pg_upgrade --link -b $B17 -B $B18 -d $OLD -D $NEW >/tmp/upgrade-link.log 2>&1
tail -6 /tmp/upgrade-link.log

echo "== 5. statistics after the upgrade, before any ANALYZE"
$B18/pg_ctl -D $NEW -o "-p 5418" -l /tmp/pg18.log start -w >/dev/null
P="psql -X -p 5418 -d bookstore"
$P -c "SELECT count(*) AS column_stats FROM pg_stats WHERE schemaname = 'public'" \
   -c "SELECT relname, last_analyze, last_autoanalyze FROM pg_stat_user_tables WHERE relname = 'orders'" \
   -c "SELECT count(*) AS extended_stats FROM pg_stats_ext"
$P -c "EXPLAIN SELECT * FROM orders WHERE status = 'Pending'" | sed -n 3p
$B18/vacuumdb -p 5418 --all --analyze-in-stages --missing-stats-only 2>&1 | grep bookstore
$P -c "SELECT count(*) AS extended_stats FROM pg_stats_ext"
$B18/pg_ctl -D $NEW stop -m fast >/dev/null
