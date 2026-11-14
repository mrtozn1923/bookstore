DECLARE @doc NVARCHAR(MAX) = CONCAT(N'{"isbn":"9798900000268","title":"Dune","blurb":"', REPLICATE(CAST(N'x' AS NVARCHAR(MAX)), 5000), N'"}');
SELECT JSON_VALUE(@doc, '$.title') AS title, JSON_VALUE(@doc, '$.blurb') AS blurb, LEN(JSON_VALUE(@doc, '$.blurb')) AS blurb_len;
GO
DECLARE @doc NVARCHAR(MAX) = CONCAT(N'{"isbn":"9798900000268","title":"Dune","blurb":"', REPLICATE(CAST(N'x' AS NVARCHAR(MAX)), 5000), N'"}');
SELECT JSON_VALUE(@doc, 'strict $.blurb') AS blurb;
GO
DECLARE @doc NVARCHAR(MAX) = CONCAT(N'{"isbn":"9798900000268","title":"Dune","blurb":"', REPLICATE(CAST(N'x' AS NVARCHAR(MAX)), 5000), N'"}');
SELECT LEN(blurb) AS blurb_len FROM OPENJSON(@doc) WITH (blurb NVARCHAR(MAX) '$.blurb');
GO
SELECT o.order_id AS id, o.status, o.ordered_at AS orderedAt,
       c.first_name + ' ' + c.last_name AS [customer.name], c.email AS [customer.email],
       (SELECT b.title, oi.quantity, oi.unit_price AS unitPrice
        FROM dbo.order_items AS oi
        JOIN dbo.books AS b ON b.book_id = oi.book_id
        WHERE oi.order_id = o.order_id
        ORDER BY oi.order_item_id
        FOR JSON PATH) AS items
FROM dbo.orders AS o
JOIN dbo.customers AS c ON c.customer_id = o.customer_id
WHERE o.order_id = 370
FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;
GO
SELECT book_id AS id, title, subtitle FROM dbo.books WHERE book_id IN (26, 28) ORDER BY book_id FOR JSON PATH;
SELECT book_id AS id, title, subtitle FROM dbo.books WHERE book_id IN (26, 28) ORDER BY book_id FOR JSON PATH, INCLUDE_NULL_VALUES, ROOT('books');
GO
SELECT b.book_id, b.title, a.last_name
FROM dbo.books AS b
JOIN dbo.book_authors AS ba ON ba.book_id = b.book_id
JOIN dbo.authors AS a ON a.author_id = ba.author_id
WHERE b.book_id IN (26, 42)
ORDER BY b.book_id, ba.author_order
FOR JSON AUTO;
GO
SELECT book_id AS id, isbn, title, list_price AS price FROM dbo.books ORDER BY book_id FOR JSON PATH;
GO
DECLARE @j NVARCHAR(MAX) = (SELECT book_id AS id, isbn, title, list_price AS price FROM dbo.books ORDER BY book_id FOR JSON PATH);
SELECT LEN(@j) AS json_length;
GO
DECLARE @order NVARCHAR(MAX) = N'{"id":370,"status":"Shipped","customer":{"name":"Annie Easley"},"items":[{"bookId":46,"quantity":1},{"bookId":25,"quantity":2}],"gift":true,"note":null}';
SELECT [key], value, type FROM OPENJSON(@order);
GO
DECLARE @orders NVARCHAR(MAX) = N'[
  {"id":501,"customerId":1,"items":[{"bookId":26,"quantity":1},{"bookId":42,"quantity":2}]},
  {"id":502,"customerId":3,"items":[{"bookId":60,"quantity":1}]}
]';
SELECT o.id AS order_id, o.customer_id, i.book_id, i.quantity
FROM OPENJSON(@orders) WITH (
         id          INT           '$.id',
         customer_id INT           '$.customerId',
         items       NVARCHAR(MAX) '$.items' AS JSON
     ) AS o
CROSS APPLY OPENJSON(o.items) WITH (book_id INT '$.bookId', quantity INT '$.quantity') AS i
ORDER BY o.id, i.book_id;
GO
DECLARE @order NVARCHAR(MAX) = N'{"id":370,"status":"Shipped","customer":{"name":"Annie Easley"},"items":[{"bookId":46,"quantity":1},{"bookId":25,"quantity":2}],"gift":true,"note":null}';
SELECT JSON_VALUE(@order, '$.customer.name') AS customer_name, JSON_VALUE(@order, '$.items[1].quantity') AS second_qty,
       JSON_VALUE(@order, '$.items') AS items_as_value, JSON_QUERY(@order, '$.items') AS items_as_query,
       JSON_VALUE(@order, '$.missing') AS missing, ISJSON(@order) AS is_json;
GO
DECLARE @order NVARCHAR(MAX) = N'{"id":370,"status":"Shipped"}';
SELECT JSON_VALUE(@order, 'strict $.missing') AS missing_strict;
GO
DECLARE @text NVARCHAR(MAX) = N'{"price":390.50}';
SELECT JSON_VALUE(@text, '$.price' RETURNING DECIMAL(10,2)) AS price;
GO
DECLARE @doc JSON = N'{"price":390.50}';
SELECT JSON_VALUE(@doc, '$.price' RETURNING DECIMAL(10,2)) AS price, JSON_VALUE(@doc, '$.price') AS price_text,
       SQL_VARIANT_PROPERTY(JSON_VALUE(@doc, '$.price'), 'BaseType') AS text_type;
GO
DECLARE @cart NVARCHAR(MAX) = N'{"customerId":1,"items":[{"bookId":26,"quantity":1}]}';
SET @cart = JSON_MODIFY(@cart, '$.items[0].quantity', 2);
SET @cart = JSON_MODIFY(@cart, 'append $.items', JSON_QUERY(N'{"bookId":42,"quantity":1}'));
SET @cart = JSON_MODIFY(@cart, '$.coupon', 'SUMMER10');
SELECT @cart AS cart;
SET @cart = JSON_MODIFY(@cart, '$.coupon', NULL);
SELECT @cart AS without_coupon;
SELECT JSON_MODIFY(N'{"items":[]}', 'append $.items', N'{"bookId":42,"quantity":1}') AS escaped;
GO
DECLARE @ids NVARCHAR(MAX) = N'[26, 42, 60]';
SELECT b.book_id, b.title, b.list_price
FROM dbo.books AS b
WHERE b.book_id IN (SELECT id FROM OPENJSON(@ids) WITH (id INT '$'))
ORDER BY b.book_id;
GO
DECLARE @cart JSON = N'{"customerId":1,"items":[{"bookId":26,"quantity":2,"price":1.00},{"bookId":42,"quantity":1},{"bookId":60,"quantity":1}]}';
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @new_order TABLE (order_id INT);
INSERT INTO dbo.orders (customer_id, ordered_at, status)
OUTPUT inserted.order_id INTO @new_order (order_id)
VALUES (JSON_VALUE(@cart, '$.customerId' RETURNING INT), SYSUTCDATETIME(), 'Pending');
INSERT INTO dbo.order_items (order_id, book_id, quantity, unit_price)
SELECT n.order_id, i.book_id, i.quantity, b.list_price
FROM OPENJSON(@cart, '$.items') WITH (book_id INT '$.bookId', quantity INT '$.quantity') AS i
JOIN dbo.books AS b ON b.book_id = i.book_id AND b.is_active = 1
CROSS JOIN @new_order AS n;
IF @@ROWCOUNT <> (SELECT COUNT(*) FROM OPENJSON(@cart, '$.items'))
    THROW 50002, 'Cart contains an unknown or inactive book.', 1;
SELECT oi.order_id, oi.book_id, b.title, oi.quantity, oi.unit_price
FROM dbo.order_items AS oi
JOIN dbo.books AS b ON b.book_id = oi.book_id
WHERE oi.order_id = (SELECT order_id FROM @new_order)
ORDER BY oi.book_id;
ROLLBACK;
GO
DROP TABLE IF EXISTS dbo.book_events;
CREATE TABLE dbo.book_events (
    event_id INT IDENTITY(1,1) CONSTRAINT PK_book_events PRIMARY KEY,
    payload  NVARCHAR(MAX) NOT NULL CONSTRAINT CK_book_events_payload CHECK (ISJSON(payload) = 1)
);
INSERT INTO dbo.book_events (payload)
SELECT CONCAT(N'{"type":"', CASE WHEN g.value % 1000 = 0 THEN 'stock_low' ELSE 'viewed' END,
              N'","bookId":', 1 + g.value % 77, N',"at":"2026-06-', FORMAT(1 + g.value % 30, '00'), N'"}')
FROM GENERATE_SERIES(1, 20000) AS g;
SELECT JSON_VALUE(payload, '$.type') AS event_type, COUNT(*) AS events FROM dbo.book_events GROUP BY JSON_VALUE(payload, '$.type');
GO
SET STATISTICS IO ON;
SELECT COUNT(*) AS low_stock_events FROM dbo.book_events WHERE JSON_VALUE(payload, '$.type') = 'stock_low';
SET STATISTICS IO OFF;
GO
ALTER TABLE dbo.book_events ADD event_type AS JSON_VALUE(payload, '$.type');
CREATE INDEX IX_book_events_event_type ON dbo.book_events (event_type);
GO
SET STATISTICS IO ON;
SELECT COUNT(*) AS low_stock_events FROM dbo.book_events WHERE JSON_VALUE(payload, '$.type') = 'stock_low';
SET STATISTICS IO OFF;
GO
SET SHOWPLAN_TEXT ON;
GO
SELECT COUNT(*) AS low_stock_events FROM dbo.book_events WHERE JSON_VALUE(payload, '$.type') = 'stock_low';
GO
SET SHOWPLAN_TEXT OFF;
GO
DROP TABLE IF EXISTS dbo.book_events_json;
CREATE TABLE dbo.book_events_json (
    event_id INT NOT NULL CONSTRAINT PK_book_events_json PRIMARY KEY,
    payload  JSON NOT NULL
);
INSERT INTO dbo.book_events_json (event_id, payload) SELECT event_id, payload FROM dbo.book_events;
CREATE JSON INDEX JX_book_events_json_payload ON dbo.book_events_json (payload) FOR ('$.type');
GO
SET STATISTICS IO ON;
SELECT COUNT(*) AS by_json_value FROM dbo.book_events_json WHERE JSON_VALUE(payload, '$.type') = 'stock_low';
SELECT COUNT(*) AS by_json_contains FROM dbo.book_events_json WHERE JSON_CONTAINS(payload, 'stock_low', '$.type') = 1;
SET STATISTICS IO OFF;
