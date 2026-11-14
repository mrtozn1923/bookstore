\set c random(1, 100000)
SELECT order_id FROM orders_p WHERE customer_id = :c ORDER BY created_at DESC LIMIT 20;
INSERT INTO orders_p (customer_id, status, total_amount, created_at) VALUES (:c, 'Pending', 99.90, now());
