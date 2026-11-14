SELECT REPLACE(N'Dune Messiah', N'd', N'X') AS replaced,
       CHARINDEX(N'MESSIAH', N'Dune Messiah') AS position_ci,
       REPLACE(N'Dune Messiah' COLLATE Latin1_General_100_CS_AS, N'd', N'X') AS replaced_cs;
GO
SELECT CHARINDEX(N'e', N'Middlemarch: A Study of Provincial Life') AS first_e,
       CHARINDEX(N'e', N'Middlemarch: A Study of Provincial Life', 7) AS next_e,
       CHARINDEX(N'x', N'Middlemarch') AS not_found,
       CHARINDEX(NULL, N'Middlemarch') AS null_search,
       PATINDEX(N'%[0-9]%', N'Fahrenheit 451') AS first_digit,
       SUBSTRING(N'Fahrenheit 451', PATINDEX(N'%[0-9]%', N'Fahrenheit 451'), 10) AS digits;
GO
SELECT LEFT(N'Dune', 10) AS left_too_long, SUBSTRING(N'Dune', 3, 100) AS sub_too_long,
       SUBSTRING(N'Dune', 0, 3) AS sub_from_zero, SUBSTRING(N'Dune', -1, 3) AS sub_negative;
GO
SELECT LEFT(N'Dune', -1) AS left_negative;
GO
DROP TABLE IF EXISTS dbo.supplier_authors;
CREATE TABLE dbo.supplier_authors (
    line_no   INT           NOT NULL PRIMARY KEY,
    full_name NVARCHAR(100) NOT NULL
);
INSERT INTO dbo.supplier_authors (line_no, full_name)
SELECT author_id, CONCAT_WS(N' ', first_name, last_name)
FROM dbo.authors
WHERE author_id IN (1, 10, 20, 28, 67);

SELECT full_name,
       CONCAT(RIGHT(full_name, CHARINDEX(N' ', REVERSE(full_name)) - 1), N', ',
              LEFT(full_name, LEN(full_name) - CHARINDEX(N' ', REVERSE(full_name)))) AS sort_name
FROM dbo.supplier_authors
ORDER BY line_no;
GO
SELECT TOP (3) email,
       STUFF(email, 2, CHARINDEX('@', email) - 2, REPLICATE('*', CHARINDEX('@', email) - 2)) AS masked
FROM dbo.customers
ORDER BY customer_id;
GO
SELECT title,
       LOWER(REPLACE(TRANSLATE(title, N'çğıöşüÇĞİÖŞÜ', N'cgiosuCGIOSU'), N' ', N'-')) AS slug
FROM dbo.books
WHERE book_id BETWEEN 74 AND 77
ORDER BY book_id;
GO
SELECT TRANSLATE(N'abc', N'ab', N'xyz') AS bad_translate;
GO
SELECT UPPER(N'istanbul') AS upper_db,
       UPPER(N'istanbul' COLLATE Turkish_100_CI_AS) AS upper_turkish,
       LOWER(N'IŞIK') AS lower_db,
       LOWER(N'IŞIK' COLLATE Turkish_100_CI_AS) AS lower_turkish;
GO
DECLARE @t TABLE (id INT, title_v VARCHAR(50), title_n NVARCHAR(50));
INSERT INTO @t VALUES
    (1, 'İnce Memed', 'İnce Memed'),
    (2, N'İnce Memed', N'İnce Memed'),
    (3, 'Tutunamayanlar ışık', N'Tutunamayanlar ışık');
SELECT id, title_v, title_n FROM @t;
GO
SELECT LEN(TRIM(CHAR(9) + N'Emma' + CHAR(13) + CHAR(10))) AS default_trim,
       LEN(TRIM(CHAR(9) + CHAR(13) + CHAR(10) + N' ' FROM CHAR(9) + N'Emma' + CHAR(13) + CHAR(10))) AS whitespace_trim;
GO
SELECT QUOTENAME(N'order items') AS bracketed,
       QUOTENAME(N'odd]name') AS escaped,
       QUOTENAME(N'O''Neil', '''') AS single_quoted,
       QUOTENAME(REPLICATE(N'x', 129)) AS too_long;
GO
SELECT customer_id, first_name, city,
       first_name + N' (' + city + N')' AS plus_op,
       CONCAT(first_name, N' (', city, N')') AS concat_fn,
       CONCAT_WS(N' / ', first_name, city, country_code) AS concat_ws_fn
FROM dbo.customers
WHERE customer_id IN (6, 7)
ORDER BY customer_id;
GO
SELECT b.book_id,
       STRING_AGG(CONCAT_WS(N' ', a.first_name, a.last_name), N'; ') AS via_concat_ws,
       STRING_AGG(a.last_name, N'; ') AS via_column
FROM dbo.books AS b
LEFT JOIN dbo.book_authors AS ba ON ba.book_id = b.book_id
LEFT JOIN dbo.authors AS a ON a.author_id = ba.author_id
WHERE b.book_id IN (24, 42)
GROUP BY b.book_id
ORDER BY b.book_id;
GO
SELECT LEN(STRING_AGG(CAST(CONCAT(o.order_id, ':', c.email) AS VARCHAR(300)), ',')) AS too_long
FROM dbo.orders AS o
JOIN dbo.customers AS c ON c.customer_id = o.customer_id;
GO
SELECT '[' + value + ']' AS value, ordinal
FROM STRING_SPLIT('12,7,,42, 26', ',', 1)
ORDER BY ordinal;
GO
DECLARE @ids VARCHAR(100) = '26,42,abc';
SELECT b.book_id, b.title
FROM dbo.books AS b
JOIN STRING_SPLIT(@ids, ',') AS s ON s.value = b.book_id;
GO
DECLARE @ids VARCHAR(100) = '26,42,abc,60, 39';
SELECT s.ordinal, s.value, b.book_id, b.title
FROM STRING_SPLIT(@ids, ',', 1) AS s
LEFT JOIN dbo.books AS b ON b.book_id = TRY_CAST(s.value AS INT)
ORDER BY s.ordinal;
GO
SET SHOWPLAN_TEXT ON;
GO
SELECT customer_id FROM dbo.customers WHERE LEFT(email, 5) = 'grace';
SELECT customer_id FROM dbo.customers WHERE email LIKE 'grace%';
GO
SET SHOWPLAN_TEXT OFF;
