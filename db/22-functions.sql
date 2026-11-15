/*
    Post 22 - functions and procedures (PostgreSQL 18).
    Run as bookstore_owner in the bookstore database, right after a fresh schema.sql:
        docker exec -it bookstore-pg psql -U bookstore_owner -d bookstore -f /lab/22-functions.sql
    The SECURITY DEFINER part continues as bookstore_app: 22-functions-app.sql.
    Leaves behind: functions isbn13_is_valid, customer_recent_orders, book_stock, book_available, line_total,
    reserve_stock, adjust_stock; constraint ck_books_isbn13 (NOT VALID); schema archive with archive.shipments;
    procedure archive_shipments; table stock_adjustments (app role has no INSERT/UPDATE/DELETE on it or stock_items).
*/
\pset footer off
\pset null '(null)'

/* guess first: the same PL/pgSQL function, VOLATILE (default) and STABLE */
CREATE FUNCTION app_customer_id() RETURNS int
LANGUAGE plpgsql
AS $$ BEGIN RETURN current_setting('app.customer_id')::int; END $$;
CREATE FUNCTION app_customer_id_stable() RETURNS int
LANGUAGE plpgsql STABLE
AS $$ BEGIN RETURN current_setting('app.customer_id')::int; END $$;
SET app.customer_id = '2';
EXPLAIN (ANALYZE, BUFFERS, COSTS OFF, TIMING OFF)
SELECT count(*) FROM orders WHERE customer_id = app_customer_id();
EXPLAIN (ANALYZE, BUFFERS, COSTS OFF, TIMING OFF)
SELECT count(*) FROM orders WHERE customer_id = app_customer_id_stable();

/* LANGUAGE sql: inlined into the query, so the marker does not matter */
CREATE FUNCTION app_customer_id_sql() RETURNS int
LANGUAGE sql
AS $$ SELECT current_setting('app.customer_id')::int $$;
EXPLAIN (COSTS OFF)
SELECT count(*) FROM orders WHERE customer_id = app_customer_id_sql();
DROP FUNCTION app_customer_id(), app_customer_id_stable(), app_customer_id_sql();

/* RETURNS TABLE, DEFAULT, named notation, LATERAL */
CREATE FUNCTION customer_recent_orders(p_customer_id int, p_limit int DEFAULT 5)
RETURNS TABLE (order_number varchar, status text, created_at timestamptz, total_amount numeric)
LANGUAGE sql STABLE
AS $$
    SELECT o.order_number, o.status, o.created_at, o.total_amount
    FROM orders AS o
    WHERE o.customer_id = p_customer_id
    ORDER BY o.created_at DESC
    LIMIT p_limit
$$;
SELECT * FROM customer_recent_orders(2, p_limit => 3);
SELECT c.customer_id, c.first_name, r.order_number, r.total_amount
FROM customers AS c
CROSS JOIN LATERAL customer_recent_orders(c.customer_id, 1) AS r
WHERE c.customer_id IN (2, 3);

/* OUT parameters; string body vs SQL-standard body */
CREATE FUNCTION book_stock(p_book_id int, OUT on_hand int, OUT available int)
LANGUAGE sql STABLE
AS $$
    SELECT quantity_on_hand, quantity_on_hand - quantity_reserved FROM stock_items WHERE book_id = p_book_id
$$;
CREATE FUNCTION book_available(p_book_id int) RETURNS int
LANGUAGE sql STABLE
RETURN (SELECT quantity_on_hand - quantity_reserved FROM stock_items WHERE book_id = p_book_id);
SELECT * FROM book_stock(1);
BEGIN;
ALTER TABLE stock_items RENAME COLUMN quantity_reserved TO reserved;
SELECT book_available(1);
SELECT * FROM book_stock(1);
ROLLBACK;
DROP TABLE stock_items;

/* volatility: index expressions need IMMUTABLE, and a wrong IMMUTABLE gives wrong results */
CREATE FUNCTION order_day(p_created_at timestamptz) RETURNS date
LANGUAGE sql
RETURN p_created_at::date;
CREATE INDEX ix_orders_order_day ON orders (order_day(created_at));
CREATE OR REPLACE FUNCTION order_day(p_created_at timestamptz) RETURNS date
LANGUAGE sql IMMUTABLE
RETURN p_created_at::date;
CREATE INDEX ix_orders_order_day ON orders (order_day(created_at));
SET TimeZone = 'Europe/Istanbul';
SELECT count(*), min(created_at), max(created_at) FROM orders WHERE order_day(created_at) = '2026-10-01';
SET enable_indexscan = off;
SET enable_bitmapscan = off;
SELECT count(*), min(created_at), max(created_at) FROM orders WHERE order_day(created_at) = '2026-10-01';
RESET enable_indexscan;
RESET enable_bitmapscan;
RESET TimeZone;
DROP INDEX ix_orders_order_day;
DROP FUNCTION order_day(timestamptz);

/* PARALLEL SAFE */
CREATE FUNCTION line_total(p_quantity int, p_unit_price numeric) RETURNS numeric
LANGUAGE plpgsql IMMUTABLE
AS $$
BEGIN
    IF p_quantity >= 3 THEN
        RETURN round(p_quantity * p_unit_price * 0.95, 2);
    END IF;
    RETURN p_quantity * p_unit_price;
END
$$;
\timing on
EXPLAIN (COSTS OFF) SELECT sum(line_total(quantity, unit_price)) FROM order_items;
SELECT sum(line_total(quantity, unit_price)) FROM order_items;
ALTER FUNCTION line_total(int, numeric) PARALLEL SAFE;
EXPLAIN (COSTS OFF) SELECT sum(line_total(quantity, unit_price)) FROM order_items;
SELECT sum(line_total(quantity, unit_price)) FROM order_items;
\timing off

/* PL/pgSQL: the ISBN-13 check digit, then a CHECK constraint */
CREATE FUNCTION isbn13_is_valid(p_isbn text) RETURNS boolean
LANGUAGE plpgsql IMMUTABLE STRICT PARALLEL SAFE
AS $$
DECLARE
    v_sum int := 0;
BEGIN
    IF p_isbn !~ '^97[89][0-9]{10}$' THEN
        RETURN false;
    END IF;
    FOR i IN 1..13 LOOP
        v_sum := v_sum + substr(p_isbn, i, 1)::int * CASE WHEN i % 2 = 0 THEN 3 ELSE 1 END;
    END LOOP;
    RETURN v_sum % 10 = 0;
END
$$;
SELECT isbn, isbn13_is_valid(isbn) AS valid
FROM (VALUES ('9780441172719'), ('9780441172718'), ('978044117271X'), (NULL)) AS v(isbn);
\timing on
SELECT count(*) FILTER (WHERE isbn13_is_valid(isbn)) AS valid, count(*) AS total FROM books;
\timing off
ALTER TABLE books ADD CONSTRAINT ck_books_isbn13 CHECK (isbn13_is_valid(isbn));
ALTER TABLE books ADD CONSTRAINT ck_books_isbn13 CHECK (isbn13_is_valid(isbn)) NOT VALID;
INSERT INTO books (isbn, title, author_id, category, price, published_year)
VALUES ('9780441172718', 'Dune #40001', 1, 'SciFi', 24.90, 1965);
UPDATE books SET price = price + 1 WHERE book_id = 1;
UPDATE books SET price = price + 1 WHERE book_id = 2;
ALTER TABLE books VALIDATE CONSTRAINT ck_books_isbn13;

/* name clash between RETURNS TABLE columns and table columns: fails at the first call, not at CREATE */
CREATE FUNCTION reserve_stock(p_order_id int)
RETURNS TABLE (book_id int, quantity int, available int)
LANGUAGE plpgsql
AS $$
DECLARE
    v_item record;
BEGIN
    FOR v_item IN
        SELECT book_id, sum(quantity)::int AS quantity
        FROM order_items
        WHERE order_id = p_order_id
        GROUP BY book_id
    LOOP
        RAISE NOTICE 'book %', v_item.book_id;
    END LOOP;
END
$$;
SELECT * FROM reserve_stock(1000000);

/* Bookstore: reserve the stock of an order in one call */
CREATE OR REPLACE FUNCTION reserve_stock(p_order_id int)
RETURNS TABLE (book_id int, quantity int, available int)
LANGUAGE plpgsql
AS $$
DECLARE
    v_status text;
    v_item   record;
BEGIN
    SELECT o.status INTO v_status FROM orders AS o WHERE o.order_id = p_order_id FOR UPDATE;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'order % not found', p_order_id USING ERRCODE = 'no_data_found';
    ELSIF v_status <> 'Pending' THEN
        RAISE EXCEPTION 'order % is %, not Pending', p_order_id, v_status USING ERRCODE = 'BK002';
    END IF;

    FOR v_item IN
        SELECT oi.book_id, sum(oi.quantity)::int AS quantity
        FROM order_items AS oi
        WHERE oi.order_id = p_order_id
        GROUP BY oi.book_id
        ORDER BY oi.book_id
    LOOP
        UPDATE stock_items AS s
        SET quantity_reserved = s.quantity_reserved + v_item.quantity,
            updated_at = now()
        WHERE s.book_id = v_item.book_id
          AND s.quantity_on_hand - s.quantity_reserved >= v_item.quantity
        RETURNING s.book_id, v_item.quantity, s.quantity_on_hand - s.quantity_reserved
        INTO book_id, quantity, available;

        IF NOT FOUND THEN
            RAISE EXCEPTION 'not enough stock for book % (order %)', v_item.book_id, p_order_id
                USING ERRCODE = 'BK001',
                      DETAIL = format('requested %s', v_item.quantity),
                      HINT = 'The order stays Pending; nothing was reserved.';
        END IF;
        RETURN NEXT;
    END LOOP;

    UPDATE orders AS o SET status = 'PaymentPending' WHERE o.order_id = p_order_id;
END
$$;
SELECT * FROM reserve_stock(1000000);
SELECT * FROM reserve_stock(1000000);
SELECT * FROM reserve_stock(999893);
SELECT book_id, quantity_on_hand, quantity_reserved FROM stock_items WHERE book_id IN (2332, 2956, 5809) ORDER BY book_id;

/* DO block: reserve every Pending order of 2026-10-20, report the ones without stock */
DO $$
DECLARE
    v_order_id int;
    v_ok       int := 0;
    v_failed   int := 0;
    v_message  text;
    v_detail   text;
BEGIN
    FOR v_order_id IN
        SELECT order_id FROM orders
        WHERE status = 'Pending' AND created_at >= '2026-10-20'
        ORDER BY order_id
    LOOP
        BEGIN
            PERFORM reserve_stock(v_order_id);
            v_ok := v_ok + 1;
        EXCEPTION
            WHEN SQLSTATE 'BK001' THEN
                GET STACKED DIAGNOSTICS v_message = MESSAGE_TEXT, v_detail = PG_EXCEPTION_DETAIL;
                v_failed := v_failed + 1;
                IF v_failed <= 3 THEN
                    RAISE NOTICE '%, %', v_message, v_detail;
                END IF;
        END;
    END LOOP;
    RAISE NOTICE 'reserved: %, not enough stock: %', v_ok, v_failed;
END
$$;
SELECT status, count(*) FROM orders WHERE created_at >= '2026-10-20' GROUP BY status ORDER BY status;

/* procedure with transaction control: nightly archiving in batches */
CREATE SCHEMA archive;
CREATE TABLE archive.shipments (LIKE public.shipments, PRIMARY KEY (shipment_id));
CREATE PROCEDURE archive_shipments(p_delivered_before timestamptz, p_batch_size int DEFAULT 10000)
LANGUAGE plpgsql
AS $$
DECLARE
    v_moved int;
    v_total int := 0;
BEGIN
    LOOP
        WITH batch AS (
            SELECT shipment_id FROM public.shipments
            WHERE delivered_at < p_delivered_before
            ORDER BY shipment_id
            LIMIT p_batch_size
        ), moved AS (
            DELETE FROM public.shipments AS s
            USING batch AS b
            WHERE s.shipment_id = b.shipment_id
            RETURNING s.*
        )
        INSERT INTO archive.shipments SELECT * FROM moved;

        GET DIAGNOSTICS v_moved = ROW_COUNT;
        EXIT WHEN v_moved = 0;
        v_total := v_total + v_moved;
        COMMIT;
        RAISE NOTICE '% rows moved, % in total', v_moved, v_total;
    END LOOP;
END
$$;
\timing on
CALL archive_shipments('2023-03-01');
\timing off
BEGIN;
CALL archive_shipments('2023-04-01');
ROLLBACK;
CREATE FUNCTION commit_inside() RETURNS void LANGUAGE plpgsql AS $$ BEGIN COMMIT; END $$;
SELECT commit_inside();
DROP FUNCTION commit_inside();
SELECT (SELECT count(*) FROM shipments) AS live, (SELECT count(*) FROM archive.shipments) AS archived;

/* SECURITY DEFINER: the app may change stock only through this function */
CREATE TABLE stock_adjustments
(
    stock_adjustment_id int GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    book_id     int         NOT NULL REFERENCES books (book_id),
    delta       int         NOT NULL,
    reason      text        NOT NULL,
    adjusted_by text        NOT NULL,
    adjusted_at timestamptz NOT NULL DEFAULT now()
);
REVOKE INSERT, UPDATE, DELETE ON stock_items, stock_adjustments FROM bookstore_app;
CREATE FUNCTION adjust_stock(p_book_id int, p_delta int, p_reason text) RETURNS int
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public, pg_temp
AS $$
DECLARE
    v_on_hand int;
BEGIN
    IF p_reason IS NULL OR length(trim(p_reason)) = 0 THEN
        RAISE EXCEPTION 'a reason is required' USING ERRCODE = 'BK003';
    END IF;
    UPDATE stock_items
    SET quantity_on_hand = quantity_on_hand + p_delta, updated_at = now()
    WHERE book_id = p_book_id
    RETURNING quantity_on_hand INTO v_on_hand;
    INSERT INTO stock_adjustments (book_id, delta, reason, adjusted_by)
    VALUES (p_book_id, p_delta, p_reason, session_user);
    RETURN v_on_hand;
END
$$;
SELECT proacl IS NULL AS default_acl FROM pg_proc WHERE proname = 'adjust_stock';
REVOKE EXECUTE ON FUNCTION adjust_stock(int, int, text) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION adjust_stock(int, int, text) TO bookstore_app;
SELECT proname, prosecdef, proconfig, proacl FROM pg_proc WHERE proname = 'adjust_stock';

/* reserve_stock also writes stock_items: same treatment */
ALTER FUNCTION reserve_stock(int) SECURITY DEFINER SET search_path = public, pg_temp;
REVOKE EXECUTE ON FUNCTION reserve_stock(int) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION reserve_stock(int) TO bookstore_app;
