/*
    Post 21 - views and materialized views (PostgreSQL 18).
    Run as bookstore_owner in the bookstore database, right after a fresh schema.sql:
        docker exec -it bookstore-cron psql -U bookstore_owner -d bookstore -f /lab/21-views.sql
    (any lab container works; the post used the pg_cron container from post 13, see 21-cron.sql)
    The security_invoker part needs a second session as bookstore_app: 21-views-app.sql.
    Leaves behind: v_book_list, v_scifi_books, customer_notes (+ RLS policy), v_customer_notes, mv_top_books.
*/
\pset footer off
\pset null '(null)'

/* guess first: SELECT * in a view, then changing the tables under it */
CREATE VIEW v_book_list AS
SELECT b.*, a.name AS author_name
FROM books AS b
JOIN authors AS a ON a.author_id = b.author_id;
ALTER TABLE books ADD COLUMN subtitle text;
SELECT * FROM v_book_list WHERE book_id = 1;
CREATE OR REPLACE VIEW v_book_list AS
SELECT b.book_id, b.isbn, b.title, a.name AS author_name
FROM books AS b
JOIN authors AS a ON a.author_id = b.author_id;
ALTER TABLE books ALTER COLUMN title TYPE varchar(300);
ALTER TABLE books DROP COLUMN subtitle;
SELECT pg_get_viewdef('v_book_list');
SELECT DISTINCT v.relname AS dependent_view
FROM pg_depend AS d
JOIN pg_rewrite AS r ON r.oid = d.objid
JOIN pg_class AS v ON v.oid = r.ev_class
WHERE d.refobjid = 'books'::regclass AND v.oid <> 'books'::regclass;

/* automatically updatable view and WITH CHECK OPTION */
CREATE VIEW v_scifi_books AS
SELECT book_id, isbn, title, author_id, category, price, published_year
FROM books
WHERE category = 'SciFi';
UPDATE v_scifi_books SET price = price + 1 WHERE book_id = 5 RETURNING book_id, title, price;
INSERT INTO v_scifi_books (isbn, title, author_id, category, price, published_year)
VALUES ('9780000040001', 'The Hobbit #40001', 1, 'Fantasy', 19.90, 1937)
RETURNING book_id;
SELECT count(*) AS visible_in_view FROM v_scifi_books WHERE book_id = 40001;
CREATE OR REPLACE VIEW v_scifi_books AS
SELECT book_id, isbn, title, author_id, category, price, published_year
FROM books
WHERE category = 'SciFi'
WITH LOCAL CHECK OPTION;
INSERT INTO v_scifi_books (isbn, title, author_id, category, price, published_year)
VALUES ('9780000040002', 'The Hobbit #40002', 1, 'Fantasy', 19.90, 1937);
UPDATE v_scifi_books SET category = 'Fantasy' WHERE book_id = 5;
CREATE VIEW v_category_stats AS
SELECT category, count(*) AS books, avg(price) AS avg_price FROM books GROUP BY category;
UPDATE v_category_stats SET books = 0 WHERE category = 'SciFi';
DROP VIEW v_category_stats;

/* views and row-level security: owner rights (default) vs security_invoker */
CREATE TABLE customer_notes
(
    note_id     int GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    customer_id int  NOT NULL REFERENCES customers (customer_id),
    note        text NOT NULL
);
INSERT INTO customer_notes (customer_id, note)
SELECT 1 + g % 5, 'note ' || g FROM generate_series(1, 50) AS g;
ALTER TABLE customer_notes ENABLE ROW LEVEL SECURITY;
CREATE POLICY customer_notes_own ON customer_notes FOR SELECT TO bookstore_app
    USING (customer_id = current_setting('app.customer_id')::int);
CREATE VIEW v_customer_notes AS SELECT customer_id, note FROM customer_notes;
CREATE VIEW v_customer_notes_invoker WITH (security_invoker = true) AS
    SELECT customer_id, note FROM customer_notes;
SELECT relname, reloptions FROM pg_class WHERE relname LIKE 'v_customer_notes%' ORDER BY 1;
