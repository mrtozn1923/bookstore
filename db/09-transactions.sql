/*
    Post 09 - transactions, isolation and locking. Run as bookstore_owner after schema.sql:
        psql -U bookstore_owner -d bookstore -f /lab/09-transactions.sql
    Creates the outbox table the .NET demos use:  dotnet run -- write-skew | queue | advisory | ef-retry
    The two-session experiments from the post are at the end as comments.
*/
\set ON_ERROR_STOP on

DROP TABLE IF EXISTS outbox_messages;
CREATE TABLE outbox_messages
(
    outbox_message_id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    type              text        NOT NULL,
    payload           jsonb       NOT NULL,
    created_at        timestamptz NOT NULL DEFAULT now(),
    processed_at      timestamptz NULL
);
/* only unprocessed rows are indexed: the relay always looks for these */
CREATE INDEX ix_outbox_messages_unprocessed ON outbox_messages (outbox_message_id) WHERE processed_at IS NULL;

INSERT INTO outbox_messages (type, payload)
SELECT 'OrderPlaced', jsonb_build_object('orderId', g)
FROM generate_series(1, 1000) AS g;

/* the write-skew demo inserts Pending orders for customer 1234; remove leftovers */
DELETE FROM orders WHERE customer_id = 1234 AND order_number LIKE 'BK-T%';

/*
    Two sessions, lost update (run once with REPEATABLE READ, once with READ COMMITTED):
        A: BEGIN ISOLATION LEVEL REPEATABLE READ; SELECT quantity_on_hand FROM stock_items WHERE book_id = 1;
        B: BEGIN ISOLATION LEVEL REPEATABLE READ; SELECT quantity_on_hand FROM stock_items WHERE book_id = 1;
        A: UPDATE stock_items SET quantity_on_hand = 35 WHERE book_id = 1; COMMIT;
        B: UPDATE stock_items SET quantity_on_hand = 35 WHERE book_id = 1;   -- 40001 under REPEATABLE READ

    FOR UPDATE vs FOR NO KEY UPDATE and foreign keys:
        A: BEGIN; SELECT book_id, price FROM books WHERE book_id = 1 FOR UPDATE;
        B: SET lock_timeout = '1s';
           BEGIN; INSERT INTO order_items (order_id, book_id, quantity, unit_price) VALUES (1, 1, 1, 10.00); ROLLBACK;
        repeat with FOR NO KEY UPDATE in A: the insert goes through.

    ALTER TABLE waiting in the lock queue:
        A: BEGIN; SELECT count(*) FROM books WHERE category = 'Horror';
        B: ALTER TABLE books ADD COLUMN subtitle text;
        C: SELECT title FROM books WHERE book_id = 2;
        D: SELECT pid, pg_blocking_pids(pid) AS blocked_by, state, wait_event_type, left(query, 45) AS query
           FROM pg_stat_activity WHERE datname = 'bookstore' AND backend_type = 'client backend';

    Deadlock: A updates book_id 1 then 2, B updates 2 then 1, inside transactions.
*/
