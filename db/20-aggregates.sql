/*
    Post 20 - aggregate, array and conditional functions (PostgreSQL 18).
    Run as bookstore_owner in the bookstore database, right after a fresh schema.sql:
        docker exec -it bookstore-pg psql -U bookstore_owner -d bookstore -f /lab/20-aggregates.sql
    NULL is shown as (null), as in the psqlrc from post 02. The demo table book_tags is dropped at the end.
*/
\pset footer off
\pset null '(null)'

/* guess first: FILTER, and aggregates over zero rows */
SELECT b.category,
       count(*) AS books,
       count(*) FILTER (WHERE s.quantity_on_hand = 0) AS out_of_stock,
       sum(s.quantity_on_hand) FILTER (WHERE b.price >= 49) AS units_premium,
       array_agg(b.book_id ORDER BY b.book_id) FILTER (WHERE s.quantity_on_hand = 0 AND b.price >= 49) AS sold_out_premium
FROM books AS b
JOIN stock_items AS s ON s.book_id = b.book_id
GROUP BY b.category
ORDER BY b.category;
SELECT count(*) AS cnt, sum(quantity_on_hand) AS total, avg(quantity_on_hand) AS average,
       array_agg(book_id) AS ids, string_agg(book_id::text, ',') AS csv, bool_and(quantity_on_hand > 0) AS all_in_stock
FROM stock_items
WHERE book_id > 1000000;

/* ordered aggregates, DISTINCT, jsonb_agg, bool_and/bool_or */
SELECT o.order_number,
       string_agg(b.title, ', ' ORDER BY oi.unit_price DESC, b.title) AS titles,
       array_agg(b.isbn ORDER BY b.isbn) AS isbns,
       jsonb_agg(jsonb_build_object('isbn', b.isbn, 'qty', oi.quantity) ORDER BY b.isbn) AS lines,
       bool_or(b.category = 'Horror') AS has_horror,
       bool_and(oi.quantity = 1) AS all_single
FROM orders AS o
JOIN order_items AS oi ON oi.order_id = o.order_id
JOIN books AS b ON b.book_id = oi.book_id
WHERE o.order_id = 1003
GROUP BY o.order_number;
SELECT array_agg(DISTINCT b.category ORDER BY b.category) AS categories,
       (array_agg(b.title ORDER BY oi.unit_price DESC))[1:2] AS top2
FROM order_items AS oi
JOIN books AS b ON b.book_id = oi.book_id
WHERE oi.order_id BETWEEN 1000 AND 1010;

/* ordered-set aggregates: median, percentile, mode */
SELECT currency,
       round(avg(total_amount), 2) AS mean,
       percentile_cont(0.5) WITHIN GROUP (ORDER BY total_amount) AS median,
       percentile_disc(0.9) WITHIN GROUP (ORDER BY total_amount) AS p90,
       mode() WITHIN GROUP (ORDER BY total_amount) AS most_common
FROM orders
WHERE created_at >= '2026-10-01'
GROUP BY currency
ORDER BY currency;

/* arrays */
SELECT t.tag, t.n FROM unnest(ARRAY['classic', 'bestseller', 'gift']) WITH ORDINALITY AS t(tag, n);
SELECT * FROM unnest(ARRAY['9780000002496', '9780000024663'], ARRAY[1, 2]) AS t(isbn, qty);
SELECT array_length('{}'::text[], 1) AS len_empty, cardinality('{}'::text[]) AS card_empty,
       array_length(ARRAY[[1, 2], [3, 4]], 1) AS dim1, cardinality(ARRAY[[1, 2], [3, 4]]) AS card_2d,
       array_append(ARRAY['classic'], 'gift') AS appended, array_remove(ARRAY['a', 'b', 'a'], 'a') AS removed,
       array_position(ARRAY['a', 'b', 'c'], 'c') AS pos, ARRAY[1, 2] || 3 AS concat;
SELECT ARRAY['classic', 'gift'] @> ARRAY['gift'] AS contains, ARRAY['classic'] && ARRAY['gift', 'classic'] AS overlaps,
       'gift' = ANY (ARRAY['classic', 'gift']) AS any_eq, 5 > ALL (ARRAY[1, 2, 3]) AS gt_all;

/* IN is = ANY; NOT IN and <> ALL with a NULL */
EXPLAIN (COSTS OFF) SELECT book_id, title FROM books WHERE isbn IN ('9780000002496', '9780000024663', '9780000031289');
PREPARE books_by_isbn(text[]) AS
    SELECT book_id, title, price FROM books WHERE isbn = ANY ($1) ORDER BY book_id;
EXPLAIN (COSTS OFF) EXECUTE books_by_isbn(ARRAY['9780000002496', '9780000024663', '9780000031289']);
EXECUTE books_by_isbn(ARRAY['9780000002496', '9780000024663', '9780000031289']);
EXECUTE books_by_isbn('{}');
DEALLOCATE books_by_isbn;
SELECT 3 NOT IN (1, 2, NULL) AS not_in_with_null, 3 <> ALL (ARRAY[1, 2, NULL]) AS ne_all_with_null,
       3 = ANY (ARRAY[1, 2, NULL]) AS any_with_null, 2 = ANY (ARRAY[1, 2, NULL]) AS any_found;
SELECT count(*) AS not_in_rows FROM books
WHERE book_id NOT IN (SELECT book_id FROM order_items WHERE order_id <= 3);
SELECT count(*) AS not_in_rows_with_null FROM books
WHERE book_id NOT IN (SELECT book_id FROM order_items WHERE order_id <= 3 UNION ALL SELECT NULL);
SELECT count(*) AS not_exists_rows_with_null FROM books AS b
WHERE NOT EXISTS (SELECT 1 FROM (SELECT book_id FROM order_items WHERE order_id <= 3 UNION ALL SELECT NULL) AS x
                  WHERE x.book_id = b.book_id);

/* GIN index for @> on an array column */
CREATE TABLE book_tags AS
SELECT book_id,
       CASE book_id % 4 WHEN 0 THEN ARRAY['classic', 'bestseller']
                        WHEN 1 THEN ARRAY['classic']
                        WHEN 2 THEN ARRAY['gift']
                        ELSE ARRAY['fantasy', 'gift'] END
       || CASE WHEN book_id % 997 = 0 THEN ARRAY['signed'] ELSE '{}' END AS tags
FROM books;
ANALYZE book_tags;
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF) SELECT count(*) FROM book_tags WHERE tags @> ARRAY['signed'];
CREATE INDEX ix_book_tags_tags ON book_tags USING gin (tags);
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF) SELECT count(*) FROM book_tags WHERE tags @> ARRAY['signed'];
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF) SELECT count(*) FROM book_tags WHERE 'signed' = ANY (tags);
DROP TABLE book_tags;

/* conditional expressions: COALESCE, NULLIF, GREATEST/LEAST, CASE */
SELECT greatest(10, NULL, 30) AS g, least(10, NULL, 30) AS l, greatest(NULL::int, NULL) AS g_all_null,
       coalesce(NULL, NULL, 'fallback') AS c, nullif(0, 0) AS n, 100 / nullif(0, 0) AS safe_div;
SELECT 100 / 0;
SELECT c.customer_id, c.created_at::date AS registered,
       max(o.created_at)::date AS last_order,
       greatest(c.created_at, max(o.created_at))::date AS last_activity,
       CASE WHEN max(o.created_at) IS NULL THEN 'never'
            WHEN max(o.created_at) < '2026-01-01' THEN 'dormant'
            ELSE 'active' END AS segment
FROM customers AS c
LEFT JOIN orders AS o ON o.customer_id = c.customer_id
WHERE c.customer_id IN (2, 99991, 99998)
GROUP BY c.customer_id, c.created_at
ORDER BY c.customer_id;

/* Bookstore: in-stock / out-of-stock per category, one query, with a share */
SELECT b.category,
       count(*) FILTER (WHERE s.quantity_on_hand - s.quantity_reserved > 0) AS in_stock,
       count(*) FILTER (WHERE s.quantity_on_hand - s.quantity_reserved <= 0) AS out_of_stock,
       round(100.0 * count(*) FILTER (WHERE s.quantity_on_hand - s.quantity_reserved <= 0)
             / nullif(count(*), 0), 2) AS out_pct
FROM books AS b
JOIN stock_items AS s ON s.book_id = b.book_id
GROUP BY ROLLUP (b.category)
ORDER BY b.category NULLS LAST;
