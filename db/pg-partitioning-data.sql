INSERT INTO orders_p SELECT * FROM orders;
INSERT INTO order_items_p (order_id, line_no, created_at, book_id, quantity, unit_price)
SELECT oi.order_id, oi.line_no, o.created_at, oi.book_id, oi.quantity, oi.unit_price
FROM order_items AS oi
JOIN orders AS o ON o.order_id = oi.order_id;
SELECT setval(pg_get_serial_sequence('orders_p', 'order_id'), (SELECT max(order_id) FROM orders_p));
VACUUM ANALYZE orders_p, order_items_p;
