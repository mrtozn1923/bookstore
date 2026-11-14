/*
    Post 11 - Npgsql and Dapper. Run as bookstore_owner after schema.sql:
        psql -U bookstore_owner -d bookstore -f /lab/11-npgsql.sql
    Then: dotnet run -- defaults | types | enum | copy | prepare | listen | otel
*/
\set ON_ERROR_STOP on

DROP TABLE IF EXISTS book_editions, order_events;
DROP TYPE IF EXISTS book_format;

/* a PostgreSQL enum mapped to a C# enum */
CREATE TYPE book_format AS ENUM ('paperback', 'hardcover', 'ebook', 'audiobook');
CREATE TABLE book_editions
(
    book_id     int         NOT NULL REFERENCES books (book_id),
    format      book_format NOT NULL,
    released_on date        NOT NULL,
    PRIMARY KEY (book_id, format)
);

/* target of the bulk load comparison */
CREATE TABLE order_events
(
    order_event_id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    order_id       int         NOT NULL,
    event_type     text        NOT NULL,
    payload        jsonb       NOT NULL,
    occurred_at    timestamptz NOT NULL
);

/* NOTIFY after a price change, delivered only when the transaction commits */
CREATE OR REPLACE FUNCTION notify_book_changed() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    PERFORM pg_notify('catalog_changed', NEW.book_id::text);
    RETURN NEW;
END $$;
CREATE OR REPLACE TRIGGER trg_books_price_changed
    AFTER UPDATE OF price ON books
    FOR EACH ROW WHEN (OLD.price IS DISTINCT FROM NEW.price)
    EXECUTE FUNCTION notify_book_changed();
