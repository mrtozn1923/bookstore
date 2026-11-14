#!/bin/bash
# Post 14 - point-in-time recovery with a base backup and a WAL archive.
# Runs inside a throwaway container as the postgres user (the server is started and stopped by this script):
#   docker build -t bookstore/pg-ops db/14-ops
#   docker run -d --name bookstore-ops --entrypoint sleep -v "${PWD}\db:/lab:ro" bookstore/pg-ops infinity
#   docker exec -u postgres -w /tmp bookstore-ops bash /lab/14-ops/pitr.sh
set -e
B=/usr/lib/postgresql/18/bin; D=/var/lib/postgresql/pitr; A=/var/lib/postgresql/archive; BK=/var/lib/postgresql/backup
P="psql -X -p 5420 -d bookstore"
rm -rf $D $D.broken $A $BK /tmp/pitr.log /tmp/pitr-restore.log; mkdir -p $A $BK
$B/initdb -D $D -U postgres --auth=trust >/dev/null
cat >> $D/postgresql.auto.conf <<CONF
archive_mode = on
archive_command = 'test ! -f $A/%f && cp %p $A/%f'
CONF
$B/pg_ctl -D $D -o "-p 5420" -l /tmp/pitr.log start -w >/dev/null
psql -X -p 5420 -q -c "CREATE ROLE bookstore_owner LOGIN" -c "CREATE ROLE bookstore_app LOGIN" -c "CREATE DATABASE bookstore OWNER bookstore_owner"
psql -X -p 5420 -q -U bookstore_owner -d bookstore -f /lab/schema.sql >/dev/null

# 1. full base backup while the server is running
$B/pg_basebackup -p 5420 -D $BK/base -X stream -c fast
du -sh $BK/base

# 2. a legitimate change, the moment we want to return to, then the mistake
$P -c "UPDATE books SET price = 41.50 WHERE book_id = 2"
sleep 2
TARGET=$($P -tAc "SELECT now()")
echo "recovery target: $TARGET"
sleep 2
$P -c "UPDATE books SET price = 9.99"
$P -tAc "SELECT pg_switch_wal()" >/dev/null
sleep 3

# 3. restore: stop, replace the data directory with the backup, replay the archive up to the target
$B/pg_ctl -D $D stop -m fast >/dev/null
mv $D $D.broken
cp -a $BK/base $D
cat >> $D/postgresql.auto.conf <<CONF
restore_command = 'cp $A/%f %p'
recovery_target_time = '$TARGET'
recovery_target_action = 'promote'
CONF
touch $D/recovery.signal
$B/pg_ctl -D $D -o "-p 5420" -l /tmp/pitr-restore.log start -w >/dev/null
sleep 2
grep -E "starting point-in-time|recovery stopping|selected new timeline" /tmp/pitr-restore.log
$P -c "SELECT book_id, price FROM books WHERE book_id IN (1, 2) ORDER BY book_id"

# 4. incremental backup (17+): needs WAL summarization, then pg_combinebackup to get a restorable directory
$P -q -c "ALTER SYSTEM SET summarize_wal = on" -c "SELECT pg_reload_conf()" >/dev/null
rm -rf $BK/*
$B/pg_basebackup -p 5420 -D $BK/full -c fast
$P -c "UPDATE stock_items SET quantity_on_hand = quantity_on_hand + 1 WHERE book_id <= 1000"
sleep 2
$B/pg_basebackup -p 5420 -D $BK/incr1 -c fast --incremental=$BK/full/backup_manifest
du -sh $BK/full $BK/incr1
$B/pg_combinebackup -o $BK/combined $BK/full $BK/incr1
du -sh $BK/combined
$B/pg_ctl -D $D stop -m fast >/dev/null
