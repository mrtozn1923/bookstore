CREATE PUBLICATION bookstore_orders FOR TABLE orders, order_items;
SELECT slot_name FROM pg_create_logical_replication_slot('reporting_sub', 'pgoutput');
