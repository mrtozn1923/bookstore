DECLARE @i INT = NULL;
SELECT ISNULL(@i, 1.5) AS isnull_result, COALESCE(@i, 1.5) AS coalesce_result;
GO
SELECT 1 + '1' AS int_plus_str, '1' + '1' AS str_plus_str, 1.5 + '1' AS dec_plus_str;
GO
SELECT customer_id FROM dbo.customers WHERE phone = 905550179553;
GO
SELECT CAST('' AS INT) AS empty_to_int, CAST('' AS DATE) AS empty_to_date, TRY_CAST('' AS INT) AS try_empty_int;
GO
DECLARE @d DATETIME2(0) = '2026-11-03 14:05:09';
SELECT CONVERT(VARCHAR(30), @d, 112) AS s112, CONVERT(VARCHAR(30), @d, 23) AS s23, CONVERT(VARCHAR(30), @d, 120) AS s120,
       CONVERT(VARCHAR(30), @d, 126) AS s126, CONVERT(VARCHAR(30), @d, 104) AS s104, CONVERT(VARCHAR(30), @d, 103) AS s103,
       CONVERT(VARCHAR(30), @d, 101) AS s101, CONVERT(VARCHAR(30), @d, 108) AS s108;
GO
SELECT CONVERT(DATE, '03.11.2026', 104) AS p104, CONVERT(DATE, '03/11/2026', 103) AS p103, CONVERT(DATE, '03/11/2026', 101) AS p101,
       CONVERT(DATE, '20261103', 112) AS p112;
GO
DROP TABLE IF EXISTS dbo.supplier_price_staging;
CREATE TABLE dbo.supplier_price_staging (
    line_no    INT          NOT NULL PRIMARY KEY,
    isbn       VARCHAR(20)  NOT NULL,
    price_text VARCHAR(30)  NULL,
    valid_from VARCHAR(30)  NULL
);
INSERT INTO dbo.supplier_price_staging (line_no, isbn, price_text, valid_from) VALUES
    (1, '9798900000268', '420,00',    '03.11.2026'),
    (2, '9798900000398', '1.234,50',  '03.11.2026'),
    (3, '9798900000602', '1450.00',   '2026-11-03'),
    (4, '9798900000589', '890,00 TL', '31.11.2026'),
    (5, '9798900000015', '',          NULL),
    (6, '9798900000022', 'N/A',       '04.11.2026');
GO
SELECT line_no, price_text,
       TRY_CAST(price_text AS DECIMAL(10,2)) AS try_cast_price,
       TRY_PARSE(price_text AS DECIMAL(10,2) USING 'tr-TR') AS try_parse_tr,
       valid_from,
       TRY_CONVERT(DATE, valid_from, 104) AS try_convert_104
FROM dbo.supplier_price_staging
ORDER BY line_no;
GO
DECLARE @p DECIMAL(10,2) = 1234.5;
SELECT CONVERT(VARCHAR(20), CAST(@p AS MONEY), 1) AS money_style1, CONVERT(VARCHAR(20), @p) AS plain,
       FORMAT(@p, 'N2', 'tr-TR') AS fmt_tr, FORMAT(@p, 'C', 'tr-TR') AS fmt_currency, FORMAT(@p, '#,0.00', 'de-DE') AS fmt_de,
       FORMAT(CAST('2026-11-03' AS DATE), 'dd MMMM yyyy dddd', 'tr-TR') AS fmt_date_tr;
GO
DROP TABLE IF EXISTS dbo.price_history;
CREATE TABLE dbo.price_history (
    history_id INT           NOT NULL PRIMARY KEY,
    price      DECIMAL(10,2) NOT NULL
);
INSERT INTO dbo.price_history (history_id, price)
SELECT value, 200 + (value % 1250) + 0.5
FROM GENERATE_SERIES(1, 200000);

SET STATISTICS TIME ON;
SELECT MAX(FORMAT(price, 'N2', 'tr-TR')) AS with_format FROM dbo.price_history;
SELECT MAX(CONVERT(VARCHAR(20), price)) AS with_convert FROM dbo.price_history;
SET STATISTICS TIME OFF;
GO
SELECT TOP (3) title, list_price,
       FORMAT(list_price, 'N2', 'tr-TR') AS with_format,
       TRANSLATE(CONVERT(VARCHAR(20), CAST(list_price AS MONEY), 1), ',.', '.,') AS with_translate
FROM dbo.books
WHERE list_price >= 1000
ORDER BY book_id;
GO
DROP TABLE IF EXISTS dbo.null_type_test;
SELECT customer_id,
       ISNULL(city, N'-') AS city_isnull,
       COALESCE(city, N'-') AS city_coalesce
INTO dbo.null_type_test
FROM dbo.customers;
SELECT c.name, TYPE_NAME(c.user_type_id) AS type_name, c.max_length, c.is_nullable
FROM sys.columns AS c
WHERE c.object_id = OBJECT_ID('dbo.null_type_test')
ORDER BY c.column_id;
DROP TABLE dbo.null_type_test;
GO
SET STATISTICS IO ON;
SELECT SUM(x) FROM (SELECT COALESCE((SELECT SUM(oi.quantity) FROM dbo.order_items AS oi WHERE oi.book_id = b.book_id), 0) AS x FROM dbo.books AS b) t;
SELECT SUM(x) FROM (SELECT ISNULL((SELECT SUM(oi.quantity) FROM dbo.order_items AS oi WHERE oi.book_id = b.book_id), 0) AS x FROM dbo.books AS b) t;
SET STATISTICS IO OFF;
GO
SELECT c.customer_id, c.first_name,
       COUNT(o.order_id) AS orders,
       SUM(IIF(o.status = 'Cancelled', 1, 0)) AS cancelled,
       CAST(100.0 * SUM(IIF(o.status = 'Cancelled', 1, 0)) / NULLIF(COUNT(o.order_id), 0) AS DECIMAL(5,1)) AS cancel_pct
FROM dbo.customers AS c
LEFT JOIN dbo.orders AS o ON o.customer_id = c.customer_id
WHERE c.customer_id IN (1, 3, 38)
GROUP BY c.customer_id, c.first_name
ORDER BY c.customer_id;
GO
SET SHOWPLAN_TEXT ON;
GO
SELECT IIF(list_price >= 1000, 'premium', 'standard') AS band,
       CHOOSE(DATEPART(QUARTER, listed_on), 'Q1', 'Q2', 'Q3', 'Q4') AS listed_quarter
FROM dbo.books;
GO
SET SHOWPLAN_TEXT OFF;
GO
SELECT CHOOSE(0, 'a', 'b') AS idx_0, CHOOSE(3, 'a', 'b') AS idx_3, CHOOSE(NULL, 'a', 'b') AS idx_null, CHOOSE(2.9, 'a', 'b', 'c') AS idx_2_9,
       IIF(NULL = NULL, 'equal', 'not equal') AS iif_null;
GO
SELECT compatibility_level FROM sys.databases WHERE name = DB_NAME();
SELECT GREATEST(1, NULL, 3) AS g_mixed, GREATEST(NULL, NULL) AS g_all_null, LEAST(CAST(NULL AS INT), 2) AS l_mixed,
       GREATEST('9', 10) AS g_types, GREATEST(N'istanbul', N'İzmir') AS g_collation;
ALTER DATABASE CURRENT SET COMPATIBILITY_LEVEL = 160;
SELECT compatibility_level FROM sys.databases WHERE name = DB_NAME();
SELECT GREATEST(1, NULL, 3) AS g_mixed, GREATEST(NULL, NULL) AS g_all_null, LEAST(CAST(NULL AS INT), 2) AS l_mixed;
ALTER DATABASE CURRENT SET COMPATIBILITY_LEVEL = 150;
SELECT GREATEST(1, NULL, 3) AS g_mixed_150;
ALTER DATABASE CURRENT SET COMPATIBILITY_LEVEL = 170;
