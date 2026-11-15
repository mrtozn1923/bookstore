/*
    Post 23 - triggers (PostgreSQL 18).
    Run as bookstore_owner in the bookstore database, right after a fresh schema.sql:
        docker exec -it bookstore-pg psql -U bookstore_owner -d bookstore -f /lab/23-triggers.sql
    The event trigger part needs the postgres superuser: 23-event-trigger.sql.
    Leaves behind: books.updated_at + trg_books_updated_at, audit_log + trg_orders_audit (row level),
    v_book_stock + trg_v_book_stock_update (INSTEAD OF). Timing demos run in transactions that are rolled back.
*/
\pset footer off
\pset null '(null)'

/* guess first: four triggers on one UPDATE */
CREATE FUNCTION trace_trigger() RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE NOTICE '% % %: %', TG_WHEN, TG_LEVEL, TG_OP, CASE WHEN TG_LEVEL = 'ROW' THEN NEW.book_id::text ELSE '-' END;
    IF TG_LEVEL = 'ROW' THEN
        RETURN NEW;
    END IF;
    RETURN NULL;
END
$$;
CREATE TRIGGER t1 BEFORE UPDATE ON books FOR EACH STATEMENT EXECUTE FUNCTION trace_trigger();
CREATE TRIGGER t2 BEFORE UPDATE ON books FOR EACH ROW EXECUTE FUNCTION trace_trigger();
CREATE TRIGGER t3 AFTER UPDATE ON books FOR EACH ROW EXECUTE FUNCTION trace_trigger();
CREATE TRIGGER t4 AFTER UPDATE ON books FOR EACH STATEMENT EXECUTE FUNCTION trace_trigger();
UPDATE books SET price = price WHERE book_id IN (1, 2);
UPDATE books SET price = price WHERE book_id = -1;
DROP TRIGGER t1 ON books;
DROP TRIGGER t2 ON books;
DROP TRIGGER t3 ON books;
DROP TRIGGER t4 ON books;
DROP FUNCTION trace_trigger();

/* updated_at with a BEFORE ROW trigger */
\timing on
ALTER TABLE books ADD COLUMN updated_at timestamptz NOT NULL DEFAULT now();
\timing off
CREATE FUNCTION set_updated_at() RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.updated_at := now();
    RETURN NEW;
END
$$;
CREATE TRIGGER trg_books_updated_at
    BEFORE UPDATE ON books
    FOR EACH ROW
    WHEN (OLD.* IS DISTINCT FROM NEW.*)
    EXECUTE FUNCTION set_updated_at();
SELECT book_id, price, updated_at FROM books WHERE book_id IN (1, 2) ORDER BY book_id;
SELECT pg_sleep(1);
UPDATE books SET price = price + 1 WHERE book_id = 1;
UPDATE books SET price = price WHERE book_id = 2;
SELECT book_id, price, updated_at FROM books WHERE book_id IN (1, 2) ORDER BY book_id;
UPDATE books SET updated_at = '2020-01-01' WHERE book_id = 2;
SELECT book_id, price, updated_at FROM books WHERE book_id = 2;

/* the classic mistake: updated_at written in an AFTER trigger that updates its own table */
CREATE FUNCTION touch_book_after() RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE books SET updated_at = now() WHERE book_id = NEW.book_id;
    RETURN NULL;
END
$$;
CREATE TRIGGER trg_books_touch AFTER UPDATE ON books FOR EACH ROW EXECUTE FUNCTION touch_book_after();
UPDATE books SET price = price + 1 WHERE book_id = 3;
DROP TRIGGER trg_books_touch ON books;
DROP FUNCTION touch_book_after();

/* audit trail: changed columns as jsonb, AFTER ROW */
CREATE TABLE audit_log
(
    audit_id   bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    table_name text        NOT NULL,
    operation  text        NOT NULL,
    row_id     int         NOT NULL,
    changes    jsonb       NOT NULL,
    changed_by text        NOT NULL DEFAULT session_user,
    changed_at timestamptz NOT NULL DEFAULT now()
);
CREATE FUNCTION audit_order_change() RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO audit_log (table_name, operation, row_id, changes)
    SELECT TG_TABLE_NAME, TG_OP, NEW.order_id,
           jsonb_object_agg(n.key, jsonb_build_object('old', to_jsonb(OLD) -> n.key, 'new', n.value))
    FROM jsonb_each(to_jsonb(NEW)) AS n
    WHERE to_jsonb(OLD) -> n.key IS DISTINCT FROM n.value;
    RETURN NULL;
END
$$;
CREATE TRIGGER trg_orders_audit
    AFTER UPDATE ON orders
    FOR EACH ROW
    WHEN (OLD.status IS DISTINCT FROM NEW.status OR OLD.total_amount IS DISTINCT FROM NEW.total_amount)
    EXECUTE FUNCTION audit_order_change();
UPDATE orders SET status = 'Shipped' WHERE order_id = 1000000;
UPDATE orders SET status = 'Shipped' WHERE order_id = 1000000;
UPDATE orders SET status = 'Delivered', total_amount = total_amount - 5 WHERE order_id = 1000000;
SELECT audit_id, row_id, changes, changed_by FROM audit_log ORDER BY audit_id;

/* cost on a bulk update of 200,000 rows: no trigger / row level / statement level with transition tables */
CREATE FUNCTION audit_order_amount_row() RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO audit_log (table_name, operation, row_id, changes)
    VALUES (TG_TABLE_NAME, TG_OP, NEW.order_id,
            jsonb_build_object('total_amount', jsonb_build_object('old', OLD.total_amount, 'new', NEW.total_amount)));
    RETURN NULL;
END
$$;
CREATE FUNCTION audit_order_amount_bulk() RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO audit_log (table_name, operation, row_id, changes)
    SELECT TG_TABLE_NAME, TG_OP, n.order_id,
           jsonb_build_object('total_amount', jsonb_build_object('old', o.total_amount, 'new', n.total_amount))
    FROM new_rows AS n
    JOIN old_rows AS o ON o.order_id = n.order_id
    WHERE o.total_amount IS DISTINCT FROM n.total_amount;
    RETURN NULL;
END
$$;
ALTER TABLE orders DISABLE TRIGGER trg_orders_audit;
CREATE TRIGGER trg_orders_amount_row
    AFTER UPDATE OF total_amount ON orders
    FOR EACH ROW
    EXECUTE FUNCTION audit_order_amount_row();
CREATE TRIGGER trg_orders_amount_bulk
    AFTER UPDATE ON orders
    REFERENCING OLD TABLE AS old_rows NEW TABLE AS new_rows
    FOR EACH STATEMENT
    EXECUTE FUNCTION audit_order_amount_bulk();
ALTER TABLE orders DISABLE TRIGGER trg_orders_amount_row;
ALTER TABLE orders DISABLE TRIGGER trg_orders_amount_bulk;
\timing on
BEGIN;
UPDATE orders SET total_amount = total_amount + 1 WHERE order_id <= 200000;
ROLLBACK;
BEGIN;
ALTER TABLE orders ENABLE TRIGGER trg_orders_amount_row;
UPDATE orders SET total_amount = total_amount + 1 WHERE order_id <= 200000;
ROLLBACK;
BEGIN;
ALTER TABLE orders ENABLE TRIGGER trg_orders_amount_bulk;
UPDATE orders SET total_amount = total_amount + 1 WHERE order_id <= 200000;
SELECT count(*) AS audit_rows FROM audit_log;
ROLLBACK;
\timing off
DROP TRIGGER trg_orders_amount_row ON orders;
DROP TRIGGER trg_orders_amount_bulk ON orders;
DROP FUNCTION audit_order_amount_row(), audit_order_amount_bulk();
ALTER TABLE orders ENABLE TRIGGER trg_orders_audit;

/* INSTEAD OF: make a join view writable */
CREATE VIEW v_book_stock AS
SELECT b.book_id, b.title, b.price, s.quantity_on_hand
FROM books AS b
JOIN stock_items AS s ON s.book_id = b.book_id;
UPDATE v_book_stock SET price = 19.90, quantity_on_hand = 50 WHERE book_id = 5;
CREATE FUNCTION v_book_stock_update() RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.book_id IS DISTINCT FROM OLD.book_id THEN
        RAISE EXCEPTION 'book_id cannot be changed through v_book_stock';
    END IF;
    UPDATE books SET title = NEW.title, price = NEW.price
    WHERE book_id = OLD.book_id
      AND (title, price) IS DISTINCT FROM (NEW.title, NEW.price);
    UPDATE stock_items SET quantity_on_hand = NEW.quantity_on_hand, updated_at = now()
    WHERE book_id = OLD.book_id
      AND quantity_on_hand IS DISTINCT FROM NEW.quantity_on_hand;
    RETURN NEW;
END
$$;
CREATE TRIGGER trg_v_book_stock_update
    INSTEAD OF UPDATE ON v_book_stock
    FOR EACH ROW
    EXECUTE FUNCTION v_book_stock_update();
UPDATE v_book_stock SET price = 19.90, quantity_on_hand = 50 WHERE book_id = 5 RETURNING *;
DELETE FROM v_book_stock WHERE book_id = 5;
SELECT b.price, s.quantity_on_hand FROM books AS b JOIN stock_items AS s USING (book_id) WHERE book_id = 5;

/* which triggers does a table have */
SELECT tgname, tgenabled, pg_get_triggerdef(oid) AS definition
FROM pg_trigger
WHERE tgrelid IN ('books'::regclass, 'orders'::regclass, 'v_book_stock'::regclass) AND NOT tgisinternal
ORDER BY tgname;
