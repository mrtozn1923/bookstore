/*
    Post 15 - one probe per line; run the same file on several major versions and compare:
        docker run -d --name pg-old -e POSTGRES_PASSWORD=<password> postgres:14-alpine
        docker cp db/15-versions.sql pg-old:/tmp/15-versions.sql
        docker exec pg-old psql -U postgres -X -f /tmp/15-versions.sql
    The probes use small throwaway tables so they also run on an empty server.
*/
\set ON_ERROR_STOP off
SELECT current_setting('server_version') AS version;
CREATE TABLE books (book_id int PRIMARY KEY, isbn text, title text, price numeric(10,2), attributes jsonb);
INSERT INTO books VALUES (1, '9780441013593', 'Dune', 32.77, '{"format": "paperback", "pages": 412}'),
                         (2, '9780547928227', 'The Hobbit', 39.00, '{"format": "ebook", "pages": 310}');
CREATE TABLE price_updates (book_id int, price numeric(10,2));
INSERT INTO price_updates VALUES (2, 41.50), (3, 12.00);
CREATE ROLE probe_app LOGIN;

/* 13: gen_random_uuid() without an extension; pg_stat_statements column names */
SELECT gen_random_uuid() IS NOT NULL AS core_uuid;
/* 14: multirange types; scram-sha-256 as the default password_encryption */
SELECT datemultirange(daterange('2026-11-01', '2026-11-10'), daterange('2026-11-20', '2026-12-01')) AS campaign_days;
SHOW password_encryption;
/* 15: MERGE, CREATE on public revoked, NULLS NOT DISTINCT, pg_backup_start, publication row filter */
MERGE INTO books b USING price_updates u ON b.book_id = u.book_id
    WHEN MATCHED THEN UPDATE SET price = u.price
    WHEN NOT MATCHED THEN INSERT (book_id, title, price) VALUES (u.book_id, 'New book', u.price);
SET ROLE probe_app;
CREATE TABLE public.probe_scratch (id int);
RESET ROLE;
CREATE TABLE coupons (code text, customer_id int, UNIQUE NULLS NOT DISTINCT (code, customer_id));
SELECT pg_start_backup('nightly');
CREATE PUBLICATION expensive_books FOR TABLE books (book_id, title, price) WHERE (price > 30);
DROP PUBLICATION IF EXISTS expensive_books;
/* 16: SQL/JSON constructors and IS JSON */
SELECT JSON_OBJECT('isbn': isbn, 'title': title) AS doc FROM books WHERE book_id = 1;
SELECT '{"pages": 412}' IS JSON OBJECT AS is_json;
/* 17: JSON_TABLE, MERGE ... RETURNING, pg_stat_checkpointer, builtin collation provider */
SELECT jt.* FROM books, JSON_TABLE(attributes, '$' COLUMNS (format text PATH '$.format', pages int PATH '$.pages')) AS jt;
MERGE INTO books b USING price_updates u ON b.book_id = u.book_id
    WHEN MATCHED THEN UPDATE SET price = u.price + 1
    RETURNING merge_action(), b.book_id, b.price;
SELECT checkpoints_timed FROM pg_stat_bgwriter;
SELECT num_timed FROM pg_stat_checkpointer;
CREATE COLLATION c_utf8 (provider = builtin, locale = 'C.UTF-8');

DROP TABLE IF EXISTS books, price_updates, coupons, public.probe_scratch;
DROP COLLATION IF EXISTS c_utf8;
DROP ROLE probe_app;
