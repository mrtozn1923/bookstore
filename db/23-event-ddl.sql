/* Post 23 - some DDL as bookstore_owner while trg_log_ddl is active */
ALTER TABLE books ADD COLUMN subtitle text;
CREATE INDEX ix_books_updated_at ON books (updated_at);
DROP INDEX ix_books_updated_at;
ALTER TABLE books DROP COLUMN subtitle;
