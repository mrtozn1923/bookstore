/* Post 23 - cost of audit triggers on a 200,000-row UPDATE, three rounds (run after 23-triggers.sql, as bookstore_owner) */
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
ALTER TABLE orders DISABLE TRIGGER trg_orders_audit;
\timing on
BEGIN;
\echo variant None
UPDATE orders SET total_amount = total_amount + 1 WHERE order_id <= 200000;
ROLLBACK;
BEGIN;
ALTER TABLE orders ENABLE TRIGGER trg_orders_amount_row;
\echo variant trg_orders_amount_row
UPDATE orders SET total_amount = total_amount + 1 WHERE order_id <= 200000;
ROLLBACK;
BEGIN;
ALTER TABLE orders ENABLE TRIGGER trg_orders_amount_bulk;
\echo variant trg_orders_amount_bulk
UPDATE orders SET total_amount = total_amount + 1 WHERE order_id <= 200000;
ROLLBACK;
BEGIN;
ALTER TABLE orders ENABLE TRIGGER trg_orders_audit;
\echo variant trg_orders_audit
UPDATE orders SET total_amount = total_amount + 1 WHERE order_id <= 200000;
ROLLBACK;
BEGIN;
\echo variant None
UPDATE orders SET total_amount = total_amount + 1 WHERE order_id <= 200000;
ROLLBACK;
BEGIN;
ALTER TABLE orders ENABLE TRIGGER trg_orders_amount_row;
\echo variant trg_orders_amount_row
UPDATE orders SET total_amount = total_amount + 1 WHERE order_id <= 200000;
ROLLBACK;
BEGIN;
ALTER TABLE orders ENABLE TRIGGER trg_orders_amount_bulk;
\echo variant trg_orders_amount_bulk
UPDATE orders SET total_amount = total_amount + 1 WHERE order_id <= 200000;
ROLLBACK;
BEGIN;
ALTER TABLE orders ENABLE TRIGGER trg_orders_audit;
\echo variant trg_orders_audit
UPDATE orders SET total_amount = total_amount + 1 WHERE order_id <= 200000;
ROLLBACK;
BEGIN;
\echo variant None
UPDATE orders SET total_amount = total_amount + 1 WHERE order_id <= 200000;
ROLLBACK;
BEGIN;
ALTER TABLE orders ENABLE TRIGGER trg_orders_amount_row;
\echo variant trg_orders_amount_row
UPDATE orders SET total_amount = total_amount + 1 WHERE order_id <= 200000;
ROLLBACK;
BEGIN;
ALTER TABLE orders ENABLE TRIGGER trg_orders_amount_bulk;
\echo variant trg_orders_amount_bulk
UPDATE orders SET total_amount = total_amount + 1 WHERE order_id <= 200000;
ROLLBACK;
BEGIN;
ALTER TABLE orders ENABLE TRIGGER trg_orders_audit;
\echo variant trg_orders_audit
UPDATE orders SET total_amount = total_amount + 1 WHERE order_id <= 200000;
ROLLBACK;
\timing off
DROP TRIGGER trg_orders_amount_row ON orders;
DROP TRIGGER trg_orders_amount_bulk ON orders;
DROP FUNCTION audit_order_amount_row(), audit_order_amount_bulk();
ALTER TABLE orders ENABLE TRIGGER trg_orders_audit;
