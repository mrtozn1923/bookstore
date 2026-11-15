/*
    Post 18 - string, regex and formatting functions (PostgreSQL 18).
    Run as bookstore_owner in the bookstore database, right after a fresh schema.sql:
        docker exec -it bookstore-pg psql -U bookstore_owner -d bookstore -f /lab/18-strings.sql
    Creates nothing permanent: pg_trgm is installed (trusted, also used in posts 06 and 10), the demo indexes are dropped.
*/
\pset footer off

/* guess first: || vs concat vs concat_ws, length in characters and bytes */
SELECT b.title || ' / ' || a.name || ' / ' || NULL AS pipe,
       concat(b.title, ' / ', a.name, ' / ', NULL) AS concat_result,
       concat_ws(' / ', b.title, a.name, NULL, b.published_year) AS concat_ws_result
FROM books AS b
JOIN authors AS a ON a.author_id = b.author_id
WHERE b.book_id = 3;
SELECT length('Çalıkuşu') AS len, octet_length('Çalıkuşu') AS bytes,
       length('Dune '::char(10)) AS char_len, octet_length('Dune '::char(10)) AS char_bytes;

/* taking strings apart */
SELECT order_number,
       split_part(order_number, '-', 2) AS digits,
       ltrim(split_part(order_number, '-', 2), '0')::int AS seq,
       split_part('2026-11-15', '-', -1) AS last_part,
       strpos(order_number, '-') AS dash_at,
       left(order_number, 2) AS prefix,
       right(order_number, 4) AS tail
FROM orders
WHERE order_id = 1042;
SELECT title,
       substring(title FROM '#(\d+)$') AS copy_no,
       substring(title, 1, strpos(title, ' #') - 1) AS base_title,
       overlay(title PLACING 'Dune Messiah' FROM 1 FOR 4) AS overlaid
FROM books
WHERE book_id = 20000;
SELECT btrim('  --Dune--  ', ' -') AS btrimmed, trim(BOTH '0' FROM '000420') AS trimmed,
       lpad('42', 8, '0') AS padded, rpad('Dune', 8, '.') || '|' AS rpadded, lpad('Middlemarch', 6) AS lpad_cut;
SELECT initcap('the hobbit, or there and back again') AS initcapped,
       translate('Moby-Dick #8', '-#', ' ') AS translated, repeat('*', 5) AS stars;
SELECT string_to_array('classic,bestseller,,gift', ',') AS arr,
       array_to_string(ARRAY['a', NULL, 'c'], ',', '?') AS joined;

/* format() */
SELECT format('Order %s for customer %s: %s %s', order_number, customer_id, total_amount, currency) AS line
FROM orders
WHERE order_id = 1042;
SELECT format('SELECT * FROM %I WHERE title = %L', 'Books', 'The Hitchhiker''s Guide') AS safe_sql,
       format('%L', NULL) AS null_literal, format('%s', NULL) AS null_s,
       format('%2$s, %1$s', 'Ada', 'Lovelace') AS positional,
       format('[%-10s][%10s]', 'Dune', 'Emma') AS widths;

/* dynamic SQL built by concatenation: the injected DROP TABLE really runs (only the foreign keys stop it) */
DO $$
DECLARE
    v_title text := 'Dune''; DROP TABLE books; --';
    v_count bigint;
BEGIN
    EXECUTE 'SELECT count(*) FROM books WHERE title = ''' || v_title || '''' INTO v_count;
END
$$;

/* the same with format(%I, %L) and with a parameter (USING) */
DO $$
DECLARE
    v_title text := 'Dune''; DROP TABLE books; --';
    v_count bigint;
BEGIN
    EXECUTE format('SELECT count(*) FROM %I WHERE title = %L', 'books', v_title) INTO v_count;
    RAISE NOTICE 'format: % rows', v_count;
    EXECUTE 'SELECT count(*) FROM books WHERE title = $1' INTO v_count USING v_title;
    RAISE NOTICE 'USING: % rows', v_count;
END
$$;

/* LIKE, ILIKE, SIMILAR TO and POSIX regex */
SELECT count(*) FILTER (WHERE title LIKE 'Dune%') AS like_cnt,
       count(*) FILTER (WHERE title ILIKE 'dune%') AS ilike_cnt,
       count(*) FILTER (WHERE title ~ '^Dune #\d{1,2}$') AS regex_cnt,
       count(*) FILTER (WHERE title ~* '^(dune|emma) ') AS regex_ci_cnt,
       count(*) FILTER (WHERE title SIMILAR TO '(Dune|Emma) #[0-9]{3}') AS similar_cnt
FROM books;
SELECT regexp_match('BK-00001042', '^([A-Z]+)-(\d+)$') AS m,
       (regexp_match('BK-00001042', '^([A-Z]+)-(\d+)$'))[2]::int AS seq;
SELECT regexp_matches('ISBN 978-0-441-17271-9, ISBN 978-0-261-10221-7', '97[89][-0-9]{14}', 'g') AS found;
SELECT regexp_replace('978-0-441-17271-9', '[^0-9]', '') AS first_only,
       regexp_replace('978-0-441-17271-9', '[^0-9]', '', 'g') AS all_digits,
       regexp_replace('Hopper, Grace', '^(\w+), (\w+)$', '\2 \1') AS swapped;
SELECT regexp_count('The Lord of the Rings', 'the', 1, 'i') AS cnt,
       regexp_like('9780441172719', '^97[89]\d{10}$') AS is_isbn,
       regexp_substr('BK-00001042', '\d+') AS sub,
       regexp_instr('Moby-Dick #8', '#') AS pos;
SELECT word, n FROM regexp_split_to_table('The Time  Machine', '\s+') WITH ORDINALITY AS t(word, n);

/* ISBN-13: format with a regex, check digit with arithmetic */
SELECT isbn,
       isbn ~ '^97[89][0-9]{10}$' AS format_ok,
       CASE WHEN isbn ~ '^97[89][0-9]{10}$' THEN
           (SELECT sum(substr(isbn, i, 1)::int * CASE WHEN i % 2 = 0 THEN 3 ELSE 1 END)
            FROM generate_series(1, 13) AS i) % 10 = 0
       END AS checksum_ok
FROM (VALUES ('9780441172719'), ('9780441172718'), ('978044117271X')) AS v(isbn);
SELECT count(*) AS total,
       count(*) FILTER (WHERE isbn ~ '^97[89][0-9]{10}$') AS format_ok,
       count(*) FILTER (WHERE (SELECT sum(substr(isbn, i, 1)::int * CASE WHEN i % 2 = 0 THEN 3 ELSE 1 END)
                               FROM generate_series(1, 13) AS i) % 10 = 0) AS checksum_ok
FROM books;

/* to_char and to_number */
SHOW lc_numeric;
SELECT to_char(1234.5, '999G999D99') AS padded, to_char(1234.5, 'FM999G999D00') AS fm,
       to_char(1234567.5, 'FM999G999D00') AS too_big, to_char(42, 'FM00000') AS zeros,
       to_char(0.256, 'FM990.0%') AS pct;
SET lc_numeric = 'tr_TR.UTF-8';
SELECT translate(to_char(1234.5, 'FM999G999G990D00'), ',.', '.,') || ' TL' AS tr_price;
SELECT to_number('1.234,50', 'FM9G999D99') AS parsed_as_en,
       to_number(translate('1.234,50', '.,', ',.'), 'FM9G999D99') AS parsed_as_tr;

/* Turkish rules: default (en_US, libc) vs the ICU tr-TR collation */
SELECT initcap('istanbul ırmak') AS default_initcap,
       initcap('istanbul ırmak' COLLATE "tr-TR-x-icu") AS tr_initcap,
       lower('KIRMIZI İNCİ') AS default_lower,
       lower('KIRMIZI İNCİ' COLLATE "tr-TR-x-icu") AS tr_lower;
SELECT 'IŞIK' ILIKE 'ışık' AS default_ilike, 'IŞIK' COLLATE "tr-TR-x-icu" ILIKE 'ışık' AS tr_ilike;

/* Bookstore: slug from a title */
SELECT title,
       trim(BOTH '-' FROM regexp_replace(
           translate(lower(title COLLATE "tr-TR-x-icu"), 'çğıöşüâî', 'cgiosuai'),
           '[^a-z0-9]+', '-', 'g')) AS slug
FROM (VALUES ('Çalıkuşu'), ('İnce Memed'), ('Kürk Mantolu Madonna'), ('The Hobbit #2'),
             ('Moby-Dick, or The Whale')) AS v(title);

/* Bookstore: monthly TRY revenue in Turkish format */
SELECT to_char(created_at, 'YYYY-MM') AS month,
       translate(to_char(sum(total_amount), 'FM999G999G990D00'), ',.', '.,') || ' TL' AS revenue
FROM orders
WHERE currency = 'TRY' AND created_at >= '2026-08-01' AND created_at < '2026-10-01'
GROUP BY 1
ORDER BY 1;

/* indexes: = vs LIKE 'prefix%' vs ILIKE '%infix%' */
CREATE INDEX ix_books_title ON books (title);
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF) SELECT book_id FROM books WHERE title = 'Dune #120';
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF) SELECT book_id FROM books WHERE title LIKE 'Dune #12%';
CREATE INDEX ix_books_title_pattern ON books (title text_pattern_ops);
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF) SELECT book_id FROM books WHERE title LIKE 'Dune #12%';
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF) SELECT book_id FROM books WHERE title ILIKE '%hobbit #12%';
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE INDEX ix_books_title_trgm ON books USING gin (title gin_trgm_ops);
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF) SELECT book_id FROM books WHERE title ILIKE '%hobbit #12%';
DROP INDEX ix_books_title, ix_books_title_pattern, ix_books_title_trgm;
