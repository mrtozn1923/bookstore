CREATE INDEX ix_orders_created ON orders (created_at);

CREATE SUBSCRIPTION reporting_sub
CONNECTION 'host=127.0.0.1 dbname=bookstore user=postgres'
PUBLICATION bookstore_orders
WITH (create_slot = false, slot_name = 'reporting_sub');
