\set order_id random(1, 1000000)
INSERT INTO order_events (order_id, kind) VALUES (:order_id, 'Paid');
