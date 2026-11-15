/*
    Post 21 - materialized view: top 100 best-selling books of the last 30 days (run after 21-views.sql):
        docker exec -it bookstore-cron psql -U bookstore_owner -d bookstore -f /lab/21-matview.sql
    The lab data ends on 2026-10-20, so "the last 30 days" is anchored to the newest order instead of now().
*/
\pset footer off
\timing on

CREATE MATERIALIZED VIEW mv_top_books AS
WITH window_start AS (
    SELECT max(created_at) - interval '30 days' AS since FROM orders
)
SELECT rank() OVER (ORDER BY sum(oi.quantity) DESC, oi.book_id) AS rank,
       oi.book_id, b.title, b.category,
       sum(oi.quantity) AS units,
       sum(oi.quantity * oi.unit_price) AS revenue
FROM order_items AS oi
JOIN orders AS o ON o.order_id = oi.order_id
JOIN books AS b ON b.book_id = oi.book_id
WHERE o.created_at >= (SELECT since FROM window_start)
  AND o.status <> 'Cancelled'
GROUP BY oi.book_id, b.title, b.category
ORDER BY units DESC, oi.book_id
LIMIT 100
WITH NO DATA;
SELECT * FROM mv_top_books LIMIT 3;
REFRESH MATERIALIZED VIEW mv_top_books;
SELECT rank, book_id, title, category, units, revenue FROM mv_top_books ORDER BY rank LIMIT 5;
SELECT pg_size_pretty(pg_total_relation_size('mv_top_books')) AS size, ispopulated
FROM pg_matviews WHERE matviewname = 'mv_top_books';
REFRESH MATERIALIZED VIEW CONCURRENTLY mv_top_books;
CREATE UNIQUE INDEX ux_mv_top_books_book_id ON mv_top_books (book_id);
REFRESH MATERIALIZED VIEW CONCURRENTLY mv_top_books;
REFRESH MATERIALIZED VIEW mv_top_books;
EXPLAIN (COSTS OFF) SELECT title, units FROM mv_top_books WHERE book_id = 9;
