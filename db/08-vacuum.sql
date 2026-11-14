/*
    Post 08 - MVCC, VACUUM and autovacuum. Run as bookstore_owner after schema.sql:
        psql -U bookstore_owner -d bookstore -f /lab/08-vacuum.sql
    Needs pgstattuple and pg_stat_scan_tables from post 03 (once, as postgres).
    The two-session experiments (idle in transaction, VACUUM FULL lock) are at the end as comments.
*/
\set ON_ERROR_STOP off

/* 1. an UPDATE of every row doubles the table; autovacuum stays out of the way during the test */
ALTER TABLE stock_items SET (autovacuum_enabled = off);
SELECT pg_size_pretty(pg_relation_size('stock_items')) AS heap, pg_relation_size('stock_items') / 8192 AS pages;
UPDATE stock_items SET quantity_reserved = quantity_reserved + 1;
SELECT pg_size_pretty(pg_relation_size('stock_items')) AS heap, pg_relation_size('stock_items') / 8192 AS pages;
SELECT pg_stat_force_next_flush();
SELECT n_live_tup, n_dead_tup, n_tup_upd, n_tup_hot_upd FROM pg_stat_user_tables WHERE relname = 'stock_items';
SELECT tuple_count, dead_tuple_count, round(dead_tuple_percent::numeric, 1) AS dead_pct,
       round(free_percent::numeric, 1) AS free_pct
FROM pgstattuple('stock_items');

/* 2. VACUUM makes the space reusable, it does not shrink the file */
VACUUM (VERBOSE) stock_items;
SELECT pg_size_pretty(pg_relation_size('stock_items')) AS heap;
SELECT tuple_count, dead_tuple_count, round(dead_tuple_percent::numeric, 1) AS dead_pct,
       round(free_percent::numeric, 1) AS free_pct
FROM pgstattuple('stock_items');
UPDATE stock_items SET quantity_reserved = quantity_reserved + 1;
SELECT pg_size_pretty(pg_relation_size('stock_items')) AS heap;
SELECT pg_stat_force_next_flush();
SELECT n_tup_upd, n_tup_hot_upd FROM pg_stat_user_tables WHERE relname = 'stock_items';

/* 3. VACUUM FULL rewrites the table (and its indexes) */
SELECT pg_size_pretty(pg_relation_size('stock_items_pkey')) AS pkey;
VACUUM FULL stock_items;
SELECT pg_size_pretty(pg_relation_size('stock_items')) AS heap, pg_size_pretty(pg_relation_size('stock_items_pkey')) AS pkey;
SELECT pg_stat_force_next_flush();
SELECT n_dead_tup FROM pg_stat_user_tables WHERE relname = 'stock_items';
/* VACUUM FULL does not reset the dead tuple counter; a plain VACUUM does */
VACUUM (ANALYZE) stock_items;

/* 4. fillfactor and HOT: ten small update rounds on two copies */
CREATE TABLE stock_ff100 AS SELECT * FROM stock_items;
CREATE TABLE stock_ff80 WITH (fillfactor = 80) AS SELECT * FROM stock_items;
ALTER TABLE stock_ff100 ADD PRIMARY KEY (book_id);
ALTER TABLE stock_ff80 ADD PRIMARY KEY (book_id);
DO $$
BEGIN
    FOR r IN 0..9 LOOP
        UPDATE stock_ff100 SET quantity_reserved = quantity_reserved + 1 WHERE book_id % 10 = r;
        UPDATE stock_ff80 SET quantity_reserved = quantity_reserved + 1 WHERE book_id % 10 = r;
        COMMIT;
    END LOOP;
END $$;
SELECT pg_stat_force_next_flush();
SELECT relname, n_tup_upd, n_tup_hot_upd, round(100.0 * n_tup_hot_upd / n_tup_upd) AS hot_pct,
       pg_size_pretty(pg_relation_size(relid)) AS heap
FROM pg_stat_user_tables WHERE relname IN ('stock_ff100', 'stock_ff80') ORDER BY 1;
DROP TABLE stock_ff100, stock_ff80;

/* 5. autovacuum thresholds per table (autovacuum_vacuum_max_threshold is new in 18) */
ALTER TABLE stock_items SET (autovacuum_enabled = on, autovacuum_vacuum_scale_factor = 0.01,
    autovacuum_vacuum_threshold = 100, log_autovacuum_min_duration = 0);
SELECT c.relname, c.reltuples::bigint,
       least(current_setting('autovacuum_vacuum_max_threshold')::bigint,
             coalesce(o.threshold, current_setting('autovacuum_vacuum_threshold')::float)
             + coalesce(o.scale, current_setting('autovacuum_vacuum_scale_factor')::float) * c.reltuples)::bigint
           AS vacuum_threshold
FROM pg_class c
LEFT JOIN LATERAL (
    SELECT max(split_part(opt, '=', 2)::float) FILTER (WHERE opt LIKE 'autovacuum_vacuum_threshold=%') AS threshold,
           max(split_part(opt, '=', 2)::float) FILTER (WHERE opt LIKE 'autovacuum_vacuum_scale_factor=%') AS scale
    FROM unnest(c.reloptions) AS opt) o ON true
WHERE c.relname IN ('orders', 'order_items', 'stock_items') ORDER BY 1;
UPDATE stock_items SET quantity_reserved = quantity_reserved + 1 WHERE book_id <= 1000;
SELECT pg_stat_force_next_flush();
SELECT n_dead_tup FROM pg_stat_user_tables WHERE relname = 'stock_items';
/* wait about a minute (autovacuum_naptime), then: */
SELECT pg_sleep(70);
SELECT relname, n_dead_tup, last_autovacuum, autovacuum_count FROM pg_stat_user_tables WHERE relname = 'stock_items';

/* 6. transaction ID age */
SELECT datname, age(datfrozenxid) AS xid_age FROM pg_database ORDER BY 2 DESC;
SELECT c.relname, age(c.relfrozenxid) AS xid_age, pg_size_pretty(pg_table_size(c.oid)) AS size
FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE c.relkind IN ('r', 'm', 't') AND n.nspname = 'public'
ORDER BY age(c.relfrozenxid) DESC LIMIT 3;

ALTER TABLE stock_items RESET (autovacuum_enabled, autovacuum_vacuum_scale_factor, autovacuum_vacuum_threshold,
    log_autovacuum_min_duration);

/*
    Two sessions: an open transaction holds VACUUM back.
    Session 1 (application role):
        BEGIN;
        UPDATE orders SET status = status WHERE order_id = 42;
        -- do nothing, leave the transaction open
    Session 2 (bookstore_owner):
        UPDATE stock_items SET quantity_reserved = quantity_reserved + 1 WHERE book_id <= 1000;
        VACUUM (VERBOSE) stock_items;          -- "1000 are dead but not yet removable"
        SELECT pid, usename, state, backend_xid, now() - xact_start AS xact_age, left(query, 50) AS last_query
        FROM pg_stat_activity WHERE state = 'idle in transaction';
    Session 1: COMMIT;  then VACUUM again in session 2.

    Two sessions: VACUUM FULL blocks readers.
    Session 1: VACUUM FULL order_items;
    Session 2: SET lock_timeout = '200ms'; SELECT count(*) FROM order_items WHERE order_id = 42;
*/
