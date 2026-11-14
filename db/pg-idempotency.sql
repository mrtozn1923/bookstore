ALTER TABLE orders ADD COLUMN idempotency_key uuid;
CREATE UNIQUE INDEX ux_orders_idempotency_key ON orders (idempotency_key);
