INSERT INTO books
SELECT g,
       lpad((9780000000000 + g)::text, 13, '0'),
       (ARRAY['Dune','Emma','The Hobbit','Middlemarch','Persuasion','Ulysses','Beloved','Walden'])[1 + g % 8] || ' #' || g,
       (ARRAY['Fantasy','Classics','Science','History','Poetry'])[1 + g % 5],
       round((80 + (g * 37) % 420)::numeric, 2)
FROM generate_series(1, 20000) AS g;

INSERT INTO customers
SELECT g,
       'customer' || g || '@example.com',
       (ARRAY['Ada','Alan','Grace','Linus','Barbara','Edsger'])[1 + g % 6] || ' ' || g,
       (ARRAY['TR','TR','TR','DE','NL','GB'])[1 + g % 6],
       timestamptz '2022-11-01' + (g * interval '20 minutes')
FROM generate_series(1, 100000) AS g;

INSERT INTO orders (customer_id, status, total_amount, currency, created_at)
SELECT 1 + (g::bigint * 7919) % 100000,
       CASE WHEN g % 50 = 0 THEN 'Cancelled' WHEN g > 990000 THEN 'Pending' ELSE 'Delivered' END,
       0,
       'TRY',
       timestamptz '2022-11-14' + (interval '4 years') * sqrt(g / 1000000.0)
FROM generate_series(1, 1000000) AS g;

INSERT INTO order_items
SELECT o.order_id, l.line_no, 1 + (o.order_id * 31 + l.line_no * 977) % 20000, 1 + (o.order_id + l.line_no) % 3, 0
FROM orders AS o
CROSS JOIN LATERAL generate_series(1, 1 + (o.order_id % 4)::int) AS l(line_no);

UPDATE order_items AS oi SET unit_price = b.price FROM books AS b WHERE b.book_id = oi.book_id;
UPDATE orders AS o SET total_amount = s.total
FROM (SELECT order_id, sum(quantity * unit_price) AS total FROM order_items GROUP BY order_id) AS s
WHERE s.order_id = o.order_id;

VACUUM ANALYZE;
