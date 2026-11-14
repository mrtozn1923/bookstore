/*
    Post 07 - reading EXPLAIN. Run as bookstore_owner after schema.sql:
        psql -U bookstore_owner -d bookstore -f /lab/07-explain.sql
    Once, as postgres (pg_stat_statements must be preloaded, so the container is restarted):
        ALTER SYSTEM SET shared_preload_libraries = 'pg_stat_statements';
        -- docker restart bookstore-pg
        \c bookstore
        CREATE EXTENSION pg_stat_statements;
        GRANT pg_read_all_stats TO bookstore_owner;
    auto_explain (optional, as postgres, reload is enough):
        ALTER SYSTEM SET session_preload_libraries = 'auto_explain';
        ALTER SYSTEM SET auto_explain.log_min_duration = '40ms';
        ALTER SYSTEM SET auto_explain.log_analyze = on;
        ALTER SYSTEM SET auto_explain.log_buffers = on;
        ALTER SYSTEM SET auto_explain.log_timing = off;
        SELECT pg_reload_conf();
    Parallel query is turned off for this session so the plans stay small and stable.
*/
\set ON_ERROR_STOP off
SET max_parallel_workers_per_gather = 0;

/* 1. estimate only: best sellers of a category in the last 30 days */
EXPLAIN
SELECT b.book_id, b.title, sum(oi.quantity) AS sold
FROM orders o
JOIN order_items oi ON oi.order_id = o.order_id
JOIN books b ON b.book_id = oi.book_id
WHERE o.created_at >= timestamptz '2026-09-20' AND o.status <> 'Cancelled' AND b.category = 'Fantasy'
GROUP BY b.book_id, b.title
ORDER BY sold DESC
LIMIT 10;

/* 2. where a cost comes from */
SELECT relpages, reltuples FROM pg_class WHERE relname = 'books';
SELECT name, setting FROM pg_settings
WHERE name IN ('seq_page_cost', 'random_page_cost', 'cpu_tuple_cost', 'cpu_operator_cost') ORDER BY 1;
EXPLAIN SELECT count(*) FROM books WHERE category = 'Fantasy';

/* 3. the same query, executed */
EXPLAIN (ANALYZE, TIMING OFF)
SELECT b.book_id, b.title, sum(oi.quantity) AS sold
FROM orders o
JOIN order_items oi ON oi.order_id = o.order_id
JOIN books b ON b.book_id = oi.book_id
WHERE o.created_at >= timestamptz '2026-09-20' AND o.status <> 'Cancelled' AND b.category = 'Fantasy'
GROUP BY b.book_id, b.title
ORDER BY sold DESC
LIMIT 10;

/* 4. ANALYZE really runs the statement: wrap DML in a transaction */
BEGIN;
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF)
DELETE FROM order_items WHERE order_id <= 1000;
ROLLBACK;
SELECT count(*) FROM order_items WHERE order_id <= 1000;

/* 5. merge join: both inputs already sorted by an index */
EXPLAIN (ANALYZE, TIMING OFF, SUMMARY OFF)
SELECT count(*) FROM orders o JOIN payments p ON p.order_id = o.order_id WHERE o.order_id <= 300000;

/* 6. an index on created_at: 30 days vs 7 days */
CREATE INDEX ix_orders_created_at ON orders (created_at);
EXPLAIN (ANALYZE, TIMING OFF, SUMMARY OFF)
SELECT b.book_id, b.title, sum(oi.quantity) AS sold
FROM orders o
JOIN order_items oi ON oi.order_id = o.order_id
JOIN books b ON b.book_id = oi.book_id
WHERE o.created_at >= timestamptz '2026-09-20' AND o.status <> 'Cancelled' AND b.category = 'Fantasy'
GROUP BY b.book_id, b.title
ORDER BY sold DESC
LIMIT 10;
EXPLAIN (ANALYZE, TIMING OFF, SUMMARY OFF)
SELECT b.book_id, b.title, sum(oi.quantity) AS sold
FROM orders o
JOIN order_items oi ON oi.order_id = o.order_id
JOIN books b ON b.book_id = oi.book_id
WHERE o.created_at >= timestamptz '2026-10-13' AND o.status <> 'Cancelled' AND b.category = 'Fantasy'
GROUP BY b.book_id, b.title
ORDER BY sold DESC
LIMIT 10;

/* 7. planner statistics */
SELECT n_distinct, most_common_vals, most_common_freqs
FROM pg_stats WHERE tablename = 'orders' AND attname = 'status';

/* 8. correlated columns: city depends on country */
EXPLAIN (ANALYZE, TIMING OFF, SUMMARY OFF)
SELECT o.currency, count(*), sum(o.total_amount)
FROM customers c JOIN orders o ON o.customer_id = c.customer_id
WHERE c.country = 'FR' AND c.city = 'Lille'
GROUP BY o.currency;
CREATE STATISTICS st_customers_country_city (dependencies) ON country, city FROM customers;
ANALYZE customers;
EXPLAIN (ANALYZE, TIMING OFF, SUMMARY OFF)
SELECT o.currency, count(*), sum(o.total_amount)
FROM customers c JOIN orders o ON o.customer_id = c.customer_id
WHERE c.country = 'FR' AND c.city = 'Lille'
GROUP BY o.currency;
SELECT dependencies FROM pg_stats_ext WHERE statistics_name = 'st_customers_country_city';

/* 9. a parameterized query as pg_stat_statements stores it */
EXPLAIN (GENERIC_PLAN)
SELECT o.book_id, sum(o.quantity) FROM order_items AS o
JOIN orders AS o0 ON o.order_id = o0.order_id
WHERE o0.created_at >= $1
GROUP BY o.book_id;

/* 10. the most expensive statements (after running: dotnet run -- bestsellers) */
SELECT calls, round(total_exec_time::numeric) AS total_ms, round(mean_exec_time::numeric, 1) AS mean_ms,
       shared_blks_hit + shared_blks_read AS blocks, left(query, 40) AS query
FROM pg_stat_statements
ORDER BY total_exec_time DESC
LIMIT 3;

/* cleanup */
DROP INDEX ix_orders_created_at;
DROP STATISTICS st_customers_country_city;
ANALYZE customers;
