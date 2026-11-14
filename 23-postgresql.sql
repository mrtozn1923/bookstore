SELECT json_build_object('id', 370, 'items', json_agg(json_build_object('bookId', b, 'quantity', q) ORDER BY b)) AS order_json
FROM (VALUES (46, 1), (25, 2)) AS v(b, q);
SELECT '{"customer":{"name":"Annie Easley"},"items":[{"bookId":46}]}'::jsonb -> 'customer' ->> 'name' AS name_op,
       JSON_VALUE('{"customer":{"name":"Annie Easley"}}'::jsonb, '$.customer.name') AS json_value_fn,
       JSON_QUERY('{"items":[{"bookId":46}]}'::jsonb, '$.items') AS json_query_fn,
       '{"a":1,}' IS JSON AS broken_is_json;
SELECT * FROM JSON_TABLE('{"items":[{"bookId":26,"quantity":2},{"bookId":42,"quantity":1}]}'::jsonb, '$.items[*]'
       COLUMNS (book_id int PATH '$.bookId', quantity int PATH '$.quantity')) AS jt;
SELECT jsonb_set('{"items":[{"bookId":26,"quantity":1}]}'::jsonb, '{items,0,quantity}', '2') AS set_qty,
       '{"customerId":1,"coupon":"SUMMER10"}'::jsonb - 'coupon' AS removed;
SELECT JSON_VALUE(json_build_object('blurb', repeat('x', 5000))::jsonb, '$.blurb') IS NOT NULL AS long_ok;
CREATE TABLE book_events (event_id int PRIMARY KEY, payload jsonb NOT NULL);
INSERT INTO book_events SELECT g, jsonb_build_object('type', CASE WHEN g % 1000 = 0 THEN 'stock_low' ELSE 'viewed' END, 'bookId', 1 + g % 77) FROM generate_series(1, 20000) g;
CREATE INDEX ix_book_events_payload ON book_events USING gin (payload jsonb_path_ops);
ANALYZE book_events;
EXPLAIN (COSTS OFF) SELECT count(*) FROM book_events WHERE payload @> '{"type":"stock_low"}';
SELECT count(*) FROM book_events WHERE payload @> '{"type":"stock_low"}';
DROP TABLE book_events;
