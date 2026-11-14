/*
    Post 06 - index types. Run as bookstore_owner after schema.sql:
        psql -U bookstore_owner -d bookstore -f /lab/06-indexes.sql
    Parallel query is turned off for this session so the plans stay small and stable.
    Every index created here is dropped at the end, except where noted.
*/
\set ON_ERROR_STOP off
SET max_parallel_workers_per_gather = 0;

/* 1. B-tree skip scan (18): leading column not in the WHERE clause */
CREATE INDEX ix_orders_status_created_at ON orders (status, created_at);
ANALYZE orders;
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF)
SELECT count(*) FROM orders
WHERE created_at >= timestamptz '2026-10-20' AND created_at < timestamptz '2026-10-21';

/* 2. deduplication */
CREATE INDEX ix_orders_status ON orders (status);
CREATE INDEX ix_orders_status_nodedup ON orders (status) WITH (deduplicate_items = off);
SELECT indexrelname, pg_size_pretty(pg_relation_size(indexrelid)) AS size
FROM pg_stat_user_indexes
WHERE indexrelname IN ('ix_orders_status', 'ix_orders_status_nodedup', 'ix_orders_status_created_at')
ORDER BY 1;
DROP INDEX ix_orders_status_nodedup, ix_orders_status, ix_orders_status_created_at;

/* 3. covering index with INCLUDE and index-only scan */
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF)
SELECT order_number, created_at, total_amount FROM orders WHERE customer_id = 42 ORDER BY created_at DESC;
CREATE INDEX ix_orders_customer_created_incl ON orders (customer_id, created_at DESC) INCLUDE (order_number, total_amount);
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF)
SELECT order_number, created_at, total_amount FROM orders WHERE customer_id = 42 ORDER BY created_at DESC;
DROP INDEX ix_orders_customer_created_incl;

/* 4. GIN with pg_trgm for ILIKE '%...%' (pg_trgm is a trusted extension) */
CREATE EXTENSION IF NOT EXISTS pg_trgm;
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF)
SELECT count(*) FROM books WHERE title ILIKE '%hobbit #12%';
CREATE INDEX ix_books_title_trgm ON books USING gin (title gin_trgm_ops);
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF)
SELECT count(*) FROM books WHERE title ILIKE '%hobbit #12%';
SELECT pg_size_pretty(pg_relation_size('ix_books_title_trgm')) AS trgm_size;
DROP INDEX ix_books_title_trgm;

/* 5. BRIN on a column that follows the physical order */
SELECT attname, correlation FROM pg_stats
WHERE tablename = 'orders' AND attname IN ('created_at', 'customer_id', 'total_amount') ORDER BY 1;
CREATE INDEX ix_orders_created_at_btree ON orders (created_at);
CREATE INDEX ix_orders_created_at_brin ON orders USING brin (created_at);
SELECT indexrelname, pg_size_pretty(pg_relation_size(indexrelid)) AS size
FROM pg_stat_user_indexes WHERE indexrelname LIKE 'ix_orders_created_at_%' ORDER BY 1;
DROP INDEX ix_orders_created_at_btree;
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF)
SELECT count(*), sum(total_amount) FROM orders
WHERE created_at >= timestamptz '2026-10-01' AND created_at < timestamptz '2026-10-02';
DROP INDEX ix_orders_created_at_brin;

/* 6. hash index */
CREATE INDEX ix_orders_order_number_hash ON orders USING hash (order_number);
SELECT indexrelname, pg_size_pretty(pg_relation_size(indexrelid)) AS size
FROM pg_stat_user_indexes WHERE indexrelname IN ('ux_orders_order_number', 'ix_orders_order_number_hash') ORDER BY 1;
EXPLAIN (COSTS OFF) SELECT * FROM orders WHERE order_number > 'BK-00999990';
DROP INDEX ix_orders_order_number_hash;

/* 7. partial index */
CREATE INDEX ix_orders_pending ON orders (created_at) WHERE status = 'Pending';
SELECT pg_size_pretty(pg_relation_size('ix_orders_pending')) AS partial_size;
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF)
SELECT order_id, created_at FROM orders WHERE status = 'Pending' ORDER BY created_at LIMIT 20;
EXPLAIN (COSTS OFF) SELECT order_id FROM orders WHERE status IN ('Pending', 'PaymentPending') ORDER BY created_at LIMIT 20;
DROP INDEX ix_orders_pending;

/* 8. expression index */
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF)
SELECT customer_id FROM customers WHERE lower(email) = 'customer42@example.com';
CREATE INDEX ix_customers_email_lower ON customers (lower(email));
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF)
SELECT customer_id FROM customers WHERE lower(email) = 'customer42@example.com';
DROP INDEX ix_customers_email_lower;

/* 9. CREATE INDEX CONCURRENTLY and an INVALID index */
CREATE UNIQUE INDEX CONCURRENTLY ux_customers_last_first ON customers (last_name, first_name);
SELECT indexrelid::regclass AS index_name, indisvalid, indisready FROM pg_index
WHERE indexrelid = 'ux_customers_last_first'::regclass;
\d customers
DROP INDEX CONCURRENTLY ux_customers_last_first;

/* 10. HOT updates and the cost of an extra index (on a copy with free space in every page) */
DROP TABLE IF EXISTS stock_demo;
CREATE TABLE stock_demo WITH (fillfactor = 80) AS SELECT * FROM stock_items;
ALTER TABLE stock_demo ADD PRIMARY KEY (book_id);
UPDATE stock_demo SET quantity_reserved = quantity_reserved + 1 WHERE book_id % 100 = 0;
SELECT pg_sleep(1);
SELECT n_tup_upd, n_tup_hot_upd FROM pg_stat_user_tables WHERE relname = 'stock_demo';
CREATE INDEX ix_stock_demo_reserved ON stock_demo (quantity_reserved);
UPDATE stock_demo SET quantity_reserved = quantity_reserved + 1 WHERE book_id % 100 = 50;
SELECT pg_sleep(1);
SELECT n_tup_upd, n_tup_hot_upd FROM pg_stat_user_tables WHERE relname = 'stock_demo';
DROP TABLE stock_demo;

/* 11. index usage statistics */
SELECT indexrelname, idx_scan, pg_size_pretty(pg_relation_size(indexrelid)) AS size
FROM pg_stat_user_indexes
WHERE schemaname = 'public' AND relname IN ('orders', 'order_items', 'books')
ORDER BY pg_relation_size(indexrelid) DESC;
