/*
    Post 05 - JSONB. Run as bookstore_owner after schema.sql:
        psql -U bookstore_owner -d bookstore -f /lab/05-jsonb.sql
    Adds books.attributes (jsonb) with deterministic content; later posts may use it.
*/
\set ON_ERROR_STOP on

ALTER TABLE books DROP COLUMN IF EXISTS attributes;
ALTER TABLE books ADD COLUMN attributes jsonb NOT NULL DEFAULT '{}';

WITH h AS (
    SELECT book_id,
           ((book_id::bigint * 48271 + 12 * 69621) % 2147483647)
         * ((book_id::bigint * 48271 + 12 * 69621) % 2147483647) % 2147483647 AS h
    FROM books
), f AS (
    SELECT book_id, h,
           CASE WHEN book_id % 10 < 5 THEN 'paperback'
                WHEN book_id % 10 < 7 THEN 'hardcover'
                WHEN book_id % 10 < 9 THEN 'ebook'
                ELSE 'audiobook' END AS format,
           CASE WHEN h % 5 = 0 THEN 'tr' ELSE 'en' END AS language
    FROM h
)
UPDATE books AS b
SET attributes = CASE f.format
    WHEN 'audiobook' THEN jsonb_build_object(
        'format', f.format, 'language', f.language,
        'duration_minutes', 300 + f.h % 900,
        'narrator', (ARRAY['Ada Lovelace', 'Alan Turing', 'Grace Hopper', 'Linus Torvalds'])[1 + (f.h / 7) % 4])
    WHEN 'ebook' THEN jsonb_build_object(
        'format', f.format, 'language', f.language,
        'pages', 120 + f.h % 700,
        'file_formats', CASE WHEN f.h % 3 = 0 THEN jsonb_build_array('epub', 'pdf') ELSE jsonb_build_array('epub') END,
        'drm', f.h % 2 = 0)
    WHEN 'hardcover' THEN jsonb_build_object(
        'format', f.format, 'language', f.language,
        'pages', 120 + f.h % 700,
        'dimensions', jsonb_build_object('width_mm', 150 + f.h % 40, 'height_mm', 220 + f.h % 30))
    ELSE jsonb_build_object(
        'format', f.format, 'language', f.language,
        'pages', 120 + f.h % 700)
    END
FROM f
WHERE f.book_id = b.book_id;

VACUUM (ANALYZE) books;
SELECT attributes->>'format' AS format, count(*) FROM books GROUP BY 1 ORDER BY 2 DESC;

\set ON_ERROR_STOP off

/* json vs jsonb */
SELECT '{"title": "Dune", "pages": 412, "pages": 896}'::json AS as_json,
       '{"title": "Dune", "pages": 412, "pages": 896}'::jsonb AS as_jsonb;
SELECT pg_column_size('{"format": "ebook", "pages": 412, "language": "en"}'::json) AS json_bytes,
       pg_column_size('{"format": "ebook", "pages": 412, "language": "en"}'::jsonb) AS jsonb_bytes;

SELECT book_id, title, attributes FROM books WHERE book_id IN (1, 6, 8, 9) ORDER BY book_id;

/* operators */
SELECT attributes -> 'pages' AS pages_jsonb, pg_typeof(attributes -> 'pages') AS t1,
       attributes ->> 'pages' AS pages_text, pg_typeof(attributes ->> 'pages') AS t2,
       attributes #>> '{dimensions,width_mm}' AS width
FROM books WHERE book_id = 6;
SELECT count(*) AS turkish_ebooks FROM books WHERE attributes @> '{"format": "ebook", "language": "tr"}';
SELECT count(*) AS with_pdf FROM books WHERE attributes -> 'file_formats' ? 'pdf';
SELECT count(*) AS has_narrator FROM books WHERE attributes ? 'narrator';
SELECT count(*) AS long_books FROM books WHERE (attributes ->> 'pages')::int > 800;
SELECT count(*) AS long_books_path FROM books WHERE attributes @? '$.pages ? (@ > 800)';

/* changing a document */
BEGIN;
UPDATE books SET attributes = jsonb_set(attributes, '{pages}', '420') WHERE book_id = 1
RETURNING old.attributes AS before, new.attributes AS after;
UPDATE books SET attributes = attributes || '{"signed": true}' WHERE book_id = 1 RETURNING new.attributes;
UPDATE books SET attributes = attributes - 'signed' WHERE book_id = 1 RETURNING new.attributes;
ROLLBACK;

/* SQL/JSON */
SELECT JSON_VALUE(attributes, '$.pages' RETURNING int) AS pages,
       JSON_QUERY(attributes, '$.file_formats') AS formats,
       JSON_EXISTS(attributes, '$.drm ? (@ == true)') AS has_drm,
       JSON_VALUE(attributes, '$.narrator' DEFAULT 'n/a' ON EMPTY) AS narrator
FROM books WHERE book_id IN (8, 9) ORDER BY book_id;
SELECT b.book_id, b.title, jt.*
FROM books AS b,
     JSON_TABLE(b.attributes, '$'
         COLUMNS (
             format  text PATH '$.format',
             pages   int  PATH '$.pages',
             minutes int  PATH '$.duration_minutes',
             NESTED PATH '$.file_formats[*]' COLUMNS (file_format text PATH '$')
         )) AS jt
WHERE b.book_id BETWEEN 6 AND 9
ORDER BY b.book_id;
SELECT JSON_VALUE('{"pages": "many"}'::jsonb, '$.pages' RETURNING int) AS pages;
SELECT JSON_VALUE('{"pages": "many"}'::jsonb, '$.pages' RETURNING int ERROR ON ERROR) AS pages;
SELECT ('{"pages": "many"}'::jsonb ->> 'pages')::int AS pages;

/* indexes */
CREATE INDEX ix_books_attributes_gin ON books USING gin (attributes);
CREATE INDEX ix_books_attributes_path ON books USING gin (attributes jsonb_path_ops);
SELECT indexrelname, pg_size_pretty(pg_relation_size(indexrelid)) AS size
FROM pg_stat_user_indexes WHERE relname = 'books' AND indexrelname LIKE 'ix_books_attr%' ORDER BY indexrelname;
DROP INDEX ix_books_attributes_path;
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF, BUFFERS OFF)
SELECT count(*) FROM books WHERE attributes @> '{"format": "ebook", "language": "tr"}';
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF, BUFFERS OFF)
SELECT count(*) FROM books WHERE attributes ? 'narrator';
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF, BUFFERS OFF)
SELECT count(*) FROM books WHERE attributes ->> 'format' = 'audiobook';
DROP INDEX ix_books_attributes_gin;
CREATE INDEX ix_books_attributes_path ON books USING gin (attributes jsonb_path_ops);
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF, BUFFERS OFF)
SELECT count(*) FROM books WHERE attributes @> '{"format": "ebook", "language": "tr"}';
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF, BUFFERS OFF)
SELECT count(*) FROM books WHERE attributes ? 'narrator';
DROP INDEX ix_books_attributes_path;
CREATE INDEX ix_books_format ON books ((attributes ->> 'format'));
CREATE INDEX ix_books_pages ON books (((attributes ->> 'pages')::int));
ANALYZE books;
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF, BUFFERS OFF)
SELECT count(*) FROM books WHERE attributes ->> 'format' = 'audiobook';
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF, BUFFERS OFF)
SELECT count(*) FROM books WHERE (attributes ->> 'pages')::int > 800;
SELECT indexrelname, pg_size_pretty(pg_relation_size(indexrelid)) AS size
FROM pg_stat_user_indexes WHERE indexrelname IN ('ix_books_format', 'ix_books_pages') ORDER BY 1;
DROP INDEX ix_books_format, ix_books_pages;

/* a large document: one small change rewrites the whole TOASTed value */
DROP TABLE IF EXISTS book_pages;
CREATE TABLE book_pages (book_id int PRIMARY KEY, view_count int NOT NULL DEFAULT 0, doc jsonb NOT NULL);
INSERT INTO book_pages (book_id, doc)
SELECT 1, jsonb_build_object(
    'view_count', 0,
    'reviews', (SELECT jsonb_agg(jsonb_build_object('customer_id', g, 'rating', 1 + g % 5,
                       'text', 'Review ' || g || ' ' || md5(g::text) || md5((g * 7)::text)))
                FROM generate_series(1, 2000) AS g));
SELECT pg_column_size(doc) AS stored_bytes, octet_length(doc::text) AS text_bytes,
       pg_size_pretty(pg_relation_size(reltoastrelid)) AS toast_size
FROM book_pages, pg_class WHERE pg_class.oid = 'book_pages'::regclass;
SELECT pg_current_wal_lsn() AS l0 \gset
UPDATE book_pages SET doc = jsonb_set(doc, '{view_count}', to_jsonb((doc ->> 'view_count')::int + 1)) WHERE book_id = 1;
SELECT pg_current_wal_lsn() AS l1 \gset
UPDATE book_pages SET view_count = view_count + 1 WHERE book_id = 1;
SELECT pg_current_wal_lsn() AS l2 \gset
SELECT pg_size_pretty(pg_wal_lsn_diff(:'l1', :'l0')) AS wal_jsonb_set,
       pg_size_pretty(pg_wal_lsn_diff(:'l2', :'l1')) AS wal_plain_column;
SELECT pg_size_pretty(pg_relation_size(reltoastrelid)) AS toast_size_after FROM pg_class WHERE oid = 'book_pages'::regclass;
DROP TABLE book_pages;

/* planner estimates on jsonb expressions */
EXPLAIN SELECT count(*) FROM books WHERE attributes ->> 'format' = 'audiobook';
CREATE STATISTICS st_books_format ON (attributes ->> 'format') FROM books;
ANALYZE books;
EXPLAIN SELECT count(*) FROM books WHERE attributes ->> 'format' = 'audiobook';
DROP STATISTICS st_books_format;

/* pitfalls */
SELECT '{"pages": 90}'::jsonb ->> 'pages' > '800' AS text_compare,
       ('{"pages": 90}'::jsonb ->> 'pages')::int > 800 AS int_compare;
SELECT count(*) AS pages_as_string FROM books WHERE attributes @> '{"pages": "388"}';
SELECT count(*) AS pages_as_number FROM books WHERE attributes @> '{"pages": 388}';
SELECT '{"narrator": null}'::jsonb -> 'narrator' AS json_null,
       ('{"narrator": null}'::jsonb -> 'narrator') IS NULL AS arrow_is_sql_null,
       ('{"narrator": null}'::jsonb ->> 'narrator') IS NULL AS text_is_sql_null;
