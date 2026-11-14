/*
    Post 03 - data types. Run as bookstore_owner in the bookstore database:
        psql -U bookstore_owner -d bookstore -f /lab/03-data-types.sql
    Several statements fail on purpose (they show an error), so ON_ERROR_STOP is off.
    The pgstatindex part needs, once, as postgres:
        CREATE EXTENSION pgstattuple; GRANT pg_stat_scan_tables TO bookstore_owner;
*/
\set ON_ERROR_STOP off

/* timestamp vs timestamptz */
DROP TABLE IF EXISTS tz_demo;
CREATE TABLE tz_demo (id int, created_ts timestamp, created_tstz timestamptz);
SET TimeZone = 'Europe/Istanbul';
INSERT INTO tz_demo VALUES (1, '2026-11-14 10:00', '2026-11-14 10:00');
SET TimeZone = 'UTC';
SELECT created_ts, created_tstz FROM tz_demo;
SET TimeZone = 'America/New_York';
SELECT created_ts, created_tstz FROM tz_demo;
SELECT pg_column_size(created_ts) AS ts_bytes, pg_column_size(created_tstz) AS tstz_bytes FROM tz_demo;
SELECT created_tstz AT TIME ZONE 'Europe/Istanbul' AS istanbul_wall_clock,
       pg_typeof(created_tstz AT TIME ZONE 'Europe/Istanbul')
FROM tz_demo;
RESET TimeZone;
DROP TABLE tz_demo;

/* text, varchar(n), char(n), domain */
DROP TABLE IF EXISTS t_len;
CREATE TABLE t_len (isbn varchar(13));
INSERT INTO t_len VALUES ('97800000000011');
INSERT INTO t_len VALUES ('9780000000001  ');
SELECT isbn, length(isbn) FROM t_len;
DROP TABLE t_len;
SELECT 'Dune'::char(10) || '|' AS char_concat, octet_length('Dune'::char(10)) AS char_bytes,
       'Dune'::char(10) = 'Dune  ' AS char_equal;
CREATE DOMAIN isbn13 AS text CHECK (VALUE ~ '^97[89][0-9]{10}$');
SELECT '9780000000001'::isbn13;
SELECT '978-0000000001'::isbn13;
DROP DOMAIN isbn13;

DROP TABLE IF EXISTS len_demo;
CREATE TABLE len_demo AS SELECT book_id, isbn, title FROM books;
SELECT pg_relation_filenode('len_demo') AS before;
ALTER TABLE len_demo ALTER COLUMN isbn TYPE varchar(20);
SELECT pg_relation_filenode('len_demo') AS after_widen;
ALTER TABLE len_demo ALTER COLUMN isbn TYPE varchar(13);
SELECT pg_relation_filenode('len_demo') AS after_shrink;
ALTER TABLE len_demo ALTER COLUMN isbn TYPE text;
SELECT pg_relation_filenode('len_demo') AS after_text;
DROP TABLE len_demo;

/* numbers */
SELECT 0.1::double precision + 0.2::double precision = 0.3 AS float_equal,
       0.1::numeric + 0.2::numeric = 0.3 AS numeric_equal;
SELECT sum(price::double precision) AS float_sum, sum(price) AS numeric_sum FROM books;
SELECT 19.905::numeric(10,2) AS rounded, 19.895::double precision::numeric(10,2) AS via_float;

/* serial vs identity */
DROP TABLE IF EXISTS wishlists_serial;
CREATE TABLE wishlists_serial (wishlist_id serial PRIMARY KEY, customer_id int NOT NULL);
INSERT INTO wishlists_serial (wishlist_id, customer_id) VALUES (1, 42);
INSERT INTO wishlists_serial (customer_id) VALUES (43);
DROP TABLE wishlists_serial;

/* uuid v4 vs v7 */
SELECT gen_random_uuid() AS v4, uuidv7() AS v7a, uuidv7() AS v7b;
SELECT uuid_extract_version(u) AS version, uuid_extract_timestamp(u) AS created
FROM (SELECT uuidv7() AS u) AS s;
DROP TABLE IF EXISTS carts_v4, carts_v7;
CREATE TABLE carts_v4 (cart_id uuid PRIMARY KEY DEFAULT gen_random_uuid(), customer_id int NOT NULL);
CREATE TABLE carts_v7 (cart_id uuid PRIMARY KEY DEFAULT uuidv7(), customer_id int NOT NULL);
INSERT INTO carts_v4 (customer_id) SELECT g FROM generate_series(1, 1000000) AS g;
INSERT INTO carts_v7 (customer_id) SELECT g FROM generate_series(1, 1000000) AS g;
SELECT 'v4' AS kind, avg_leaf_density, leaf_pages FROM pgstatindex('carts_v4_pkey')
UNION ALL
SELECT 'v7', avg_leaf_density, leaf_pages FROM pgstatindex('carts_v7_pkey');
DROP TABLE carts_v4, carts_v7;

/* enum */
DROP TABLE IF EXISTS status_demo;
DROP TYPE IF EXISTS order_status;
CREATE TYPE order_status AS ENUM ('Pending', 'PaymentPending', 'Shipped', 'Delivered', 'Cancelled');
CREATE TABLE status_demo (order_id int, status order_status);
INSERT INTO status_demo VALUES (1, 'Shipped'), (2, 'Pending'), (3, 'Cancelled');
SELECT order_id, status FROM status_demo ORDER BY status;
INSERT INTO status_demo VALUES (4, 'Returned');
ALTER TYPE order_status ADD VALUE 'Returned' BEFORE 'Cancelled';
ALTER TYPE order_status DROP VALUE 'Returned';
DROP TABLE status_demo;
DROP TYPE order_status;

/* arrays */
DROP TABLE IF EXISTS book_tags;
CREATE TABLE book_tags AS SELECT book_id, title FROM books WHERE book_id <= 6;
ALTER TABLE book_tags ADD COLUMN tags text[] NOT NULL DEFAULT '{}';
UPDATE book_tags SET tags = CASE book_id % 3 WHEN 0 THEN ARRAY['classic', 'bestseller']
                                             WHEN 1 THEN ARRAY['classic']
                                             ELSE ARRAY['fantasy', 'bestseller', 'gift'] END;
SELECT book_id FROM book_tags WHERE 'gift' = ANY (tags) ORDER BY 1;
SELECT book_id FROM book_tags WHERE tags @> ARRAY['classic', 'bestseller'] ORDER BY 1;
SELECT tag, count(*) FROM book_tags, unnest(tags) AS tag GROUP BY tag ORDER BY 2 DESC, 1;
DROP TABLE book_tags;

DROP TABLE IF EXISTS wishlists;
CREATE TABLE wishlists (wishlist_id int GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                        book_ids int[] REFERENCES books (book_id));
CREATE TABLE wishlists (wishlist_id int GENERATED ALWAYS AS IDENTITY PRIMARY KEY, book_ids int[] NOT NULL);
INSERT INTO wishlists (book_ids) VALUES (ARRAY[1, 2, 999999]);
SELECT w.wishlist_id, id AS book_id, b.title
FROM wishlists AS w CROSS JOIN LATERAL unnest(w.book_ids) AS id
LEFT JOIN books AS b ON b.book_id = id;
DROP TABLE wishlists;

/* ranges: campaigns stays for post 04 */
DROP TABLE IF EXISTS campaigns;
CREATE TABLE campaigns
(
    campaign_id int GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name        text NOT NULL,
    category    text NOT NULL,
    discount    numeric(4,2) NOT NULL,
    period      daterange NOT NULL
);
INSERT INTO campaigns (name, category, discount, period) VALUES
    ('Back to School', 'Classic', 0.10, '[2026-09-01,2026-09-15)'),
    ('Black Friday',   'SciFi',   0.25, '[2026-11-27,2026-11-30]'),
    ('Winter Reads',   'Classic', 0.15, '[2026-12-01,2027-01-01)');
SELECT name, period FROM campaigns ORDER BY campaign_id;
SELECT name, discount FROM campaigns WHERE period @> date '2026-11-30';
SELECT name FROM campaigns WHERE period && daterange('2026-11-29', '2026-12-03');
SELECT range_agg(period) AS classic_days FROM campaigns WHERE category = 'Classic';

/* collation and Turkish */
SELECT collname, collprovider, colllocale FROM pg_collation
WHERE collname IN ('tr-x-icu', 'und-x-icu', 'pg_c_utf8', 'pg_unicode_fast', 'en_US.utf8');
SELECT upper('istanbul') AS default_upper,
       upper('istanbul' COLLATE "tr-x-icu") AS tr_upper,
       lower('ISPARTA') AS default_lower,
       lower('ISPARTA' COLLATE "tr-x-icu") AS tr_lower;
DROP TABLE IF EXISTS words;
CREATE TABLE words (w text);
INSERT INTO words VALUES ('Cam'), ('Çiçek'), ('Dut'), ('Ilık'), ('İnci'), ('Işık'), ('Su'), ('Şeker'), ('Tuz'), ('Zil');
SELECT string_agg(w, ' ' ORDER BY w) AS default_en_us,
       string_agg(w, ' ' ORDER BY w COLLATE "tr-x-icu") AS tr_icu,
       string_agg(w, ' ' ORDER BY w COLLATE "C") AS c_bytes
FROM words;
DROP TABLE words;
DROP TABLE IF EXISTS ci_demo;
DROP COLLATION IF EXISTS tr_ci;
CREATE COLLATION tr_ci (provider = icu, locale = 'tr-TR-u-ks-level2', deterministic = false);
CREATE TABLE ci_demo (email text COLLATE tr_ci UNIQUE);
INSERT INTO ci_demo VALUES ('Ada@Example.com');
INSERT INTO ci_demo VALUES ('ada@example.com');
SELECT email FROM ci_demo WHERE email LIKE 'ada@%';
SELECT 'ISTANBUL' = 'istanbul' COLLATE tr_ci AS i_eq, 'İSTANBUL' = 'istanbul' COLLATE tr_ci AS dotted_i_eq;
DROP TABLE ci_demo;
DROP COLLATION tr_ci;
SELECT casefold('Straße' COLLATE "pg_unicode_fast") AS casefold_fast,
       lower('Straße' COLLATE "pg_unicode_fast") AS lower_fast,
       casefold('ISPARTA' COLLATE "tr-x-icu") AS casefold_tr;
