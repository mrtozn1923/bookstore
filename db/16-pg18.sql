/*
    Post 16 - PostgreSQL 18 features next to their PostgreSQL 17 workarounds.
    Run as bookstore_owner after a fresh schema.sql (the same file also runs on 17; the 18-only lines fail there):
        psql -U bookstore_owner -d bookstore -f /lab/16-pg18.sql
    Asynchronous I/O measurement (as postgres, needs a restart per io_method):
        ALTER SYSTEM SET io_method = 'sync';   -- or 'worker' (default); docker restart bookstore-pg
        CREATE EXTENSION pg_buffercache;
        SET max_parallel_workers_per_gather = 0;
        SELECT count(*) FROM pg_buffercache_evict_relation('order_items');
        EXPLAIN (ANALYZE, BUFFERS, TIMING OFF) SELECT count(*) FROM order_items;
        ALTER SYSTEM RESET io_method; DROP EXTENSION pg_buffercache;
*/
\set ON_ERROR_STOP off
\timing on

/* 1. generated columns: virtual is the default in 18, 17 needs STORED */
SELECT pg_size_pretty(pg_relation_size('orders')) AS before;
ALTER TABLE orders ADD COLUMN total_with_vat numeric(12,2) GENERATED ALWAYS AS (round(total_amount * 1.20, 2));
CREATE INDEX ix_orders_total_with_vat ON orders (total_with_vat);
EXPLAIN (VERBOSE, COSTS OFF) SELECT total_with_vat FROM orders WHERE order_id = 1;
ALTER TABLE orders ADD COLUMN total_with_vat_stored numeric(12,2) GENERATED ALWAYS AS (round(total_amount * 1.20, 2)) STORED;
SELECT pg_size_pretty(pg_relation_size('orders')) AS after;
ALTER TABLE orders DROP COLUMN IF EXISTS total_with_vat, DROP COLUMN IF EXISTS total_with_vat_stored;
\timing off

/* 2. EXPLAIN: BUFFERS by default and fractional rows in 18 */
EXPLAIN (ANALYZE, TIMING OFF, COSTS OFF) SELECT count(*) FROM books WHERE category = 'Horror';

/* 3. uuidv7() */
SELECT uuidv7() AS id, uuid_extract_timestamp(uuidv7()) AS ts, uuid_extract_version(uuidv7()) AS v;

/* 4. RETURNING OLD/NEW (18) and the CTE that does the same on 17 */
BEGIN;
UPDATE books SET price = round(price * 1.05, 2)
WHERE book_id IN (1, 2, 3)
RETURNING book_id, old.price AS old_price, new.price AS new_price;
ROLLBACK;
BEGIN;
WITH before AS (
    SELECT book_id, price FROM books WHERE book_id IN (1, 2, 3) FOR UPDATE
)
UPDATE books b SET price = round(b.price * 1.05, 2)
FROM before
WHERE b.book_id = before.book_id
RETURNING b.book_id, before.price AS old_price, b.price AS new_price;
ROLLBACK;

/* 5. temporal primary key (18, needs btree_gist from post 04) */
CREATE TEMP TABLE book_prices_demo (book_id int, valid_during daterange, price numeric(10,2),
    PRIMARY KEY (book_id, valid_during WITHOUT OVERLAPS));

/* 6. settings that changed */
SHOW data_checksums;
SHOW io_method;
