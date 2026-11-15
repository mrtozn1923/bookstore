/*
    Post 24 - everyday work with psql (PostgreSQL 18).
    This file collects the server-side SQL the post runs; the meta-commands (\d, \x, \copy, \watch, \gexec) are shown
    in the post and typed interactively. Connect as bookstore_app for reads, bookstore_owner for DDL / \gexec.
    Nothing here is left behind: the staging schema and the demo indexes are dropped at the end.
*/
\pset footer off
\pset null '(null)'

/* \gset: capture a single-row result into variables, then use them */
SELECT count(*) AS total_books, max(book_id) AS max_id FROM books \gset
\echo total books: :total_books max id: :max_id

/* variables in a query */
\set low_stock 10
SELECT count(*) AS below FROM stock_items WHERE quantity_on_hand < :low_stock;

/* \gexec: run the SQL produced by a query (as bookstore_owner) */
SELECT format('ANALYZE %I;', tablename) FROM pg_tables WHERE schemaname = 'public' AND tablename LIKE 'order%' \gexec

/* search_path trap: the same table name in two schemas */
CREATE SCHEMA staging;
CREATE TABLE staging.books (book_id int, title text, note text);
INSERT INTO staging.books VALUES (1, 'Emma #1 (draft)', 'import test');
SHOW search_path;
SELECT title FROM books WHERE book_id = 1;
SET search_path = staging, public;
SELECT title FROM books WHERE book_id = 1;
SELECT title FROM public.books WHERE book_id = 1;
RESET search_path;
DROP SCHEMA staging CASCADE;

/* public schema: the app role cannot create objects in it since PostgreSQL 15 */
SELECT nspname, pg_catalog.has_schema_privilege('bookstore_app', nspname, 'CREATE') AS app_can_create
FROM pg_namespace WHERE nspname = 'public';
