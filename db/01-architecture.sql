/*
    Post 01 - architecture demos (run as postgres in the throwaway pg-arch container).
    psql -U postgres -f 01-architecture.sql   (the MVCC part needs a second session, see the post)
*/
SELECT pid, backend_type FROM pg_stat_activity ORDER BY backend_type, pid;

/* MVCC: two versions of one row */
DROP TABLE IF EXISTS stock_items;
CREATE TABLE stock_items (book_id int PRIMARY KEY, quantity_on_hand int NOT NULL);
INSERT INTO stock_items VALUES (1, 36), (2, 12);
SELECT ctid, xmin, xmax, * FROM stock_items;

CREATE EXTENSION IF NOT EXISTS pageinspect;
UPDATE stock_items SET quantity_on_hand = 35 WHERE book_id = 1;
SELECT lp, t_xmin, t_xmax, t_ctid FROM heap_page_items(get_raw_page('stock_items', 0));
SELECT n_live_tup, n_dead_tup FROM pg_stat_user_tables WHERE relname = 'stock_items';
VACUUM stock_items;
SELECT lp, lp_flags, t_xmin, t_xmax, t_ctid FROM heap_page_items(get_raw_page('stock_items', 0));

/* WAL */
SELECT pg_current_wal_lsn() AS before_lsn \gset
INSERT INTO stock_items SELECT g, g % 200 FROM generate_series(3, 100002) AS g;
SELECT pg_current_wal_lsn() AS after_lsn,
       pg_size_pretty(pg_wal_lsn_diff(pg_current_wal_lsn(), :'before_lsn')) AS wal_written,
       pg_size_pretty(pg_relation_size('stock_items')) AS table_size,
       pg_walfile_name(pg_current_wal_lsn()) AS wal_file;
SELECT num_timed, num_requested, buffers_written FROM pg_stat_checkpointer;
CHECKPOINT;
SELECT num_timed, num_requested, buffers_written FROM pg_stat_checkpointer;

/* shared_buffers vs the operating system page cache */
CREATE EXTENSION IF NOT EXISTS pg_buffercache;
SELECT buffers_evicted FROM pg_buffercache_evict_relation('stock_items');
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF) SELECT sum(quantity_on_hand) FROM stock_items;
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF) SELECT sum(quantity_on_hand) FROM stock_items;
