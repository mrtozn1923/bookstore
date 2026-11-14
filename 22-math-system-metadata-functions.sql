SELECT ROUND(CAST(9.99 AS DECIMAL(3,2)), 1) AS overflow;
GO
SELECT ROUND(2.345, 2) AS r2, ROUND(2.345, 2, 1) AS truncated, ROUND(-2.5, 0) AS neg_half, ROUND(2.5, 0) AS pos_half,
       ROUND(1234.5, -2) AS to_hundreds, ROUND(CAST(2.5 AS FLOAT), 0) AS float_half;
GO
SELECT CEILING(2.1) AS ceil_pos, CEILING(-2.1) AS ceil_neg, FLOOR(-2.1) AS floor_neg, ABS(-7) AS abs_val,
       POWER(2, 0.5) AS pow_int_base, POWER(2.0, 0.5) AS pow_dec_base, SQRT(2) AS sqrt_val, SIGN(-3.5) AS sign_val, 17 % 5 AS modulo;
GO
SELECT 7 / 2 AS int_div, 7 / 2.0 AS dec_div, 7 % 2 AS remainder, CAST(7 AS DECIMAL(10,2)) / 3 AS dec_by_int;
GO
SELECT AVG(rating) AS avg_int, AVG(CAST(rating AS DECIMAL(5,2))) AS avg_dec, AVG(rating * 1.0) AS avg_times_one
FROM dbo.reviews
WHERE book_id = 26;
GO
DECLARE @a DECIMAL(10,2) = 1234.56, @b DECIMAL(10,2) = 3;
SELECT @a * @b AS product, @a / @b AS quotient,
       SQL_VARIANT_PROPERTY(@a * @b, 'Precision') AS p_prec, SQL_VARIANT_PROPERTY(@a * @b, 'Scale') AS p_scale,
       SQL_VARIANT_PROPERTY(@a / @b, 'Precision') AS q_prec, SQL_VARIANT_PROPERTY(@a / @b, 'Scale') AS q_scale;
GO
DECLARE @x DECIMAL(38,10) = 1, @y DECIMAL(38,10) = 3;
SELECT @x / @y AS big_div, SQL_VARIANT_PROPERTY(@x / @y, 'Scale') AS scale;
GO
SELECT TOP (3) book_id, RAND() AS rand_value, RAND(CHECKSUM(NEWID())) AS rand_per_row
FROM dbo.books
ORDER BY book_id;
GO
SET SHOWPLAN_TEXT ON;
GO
SELECT TOP (1) book_id, title FROM dbo.books WHERE is_active = 1 ORDER BY NEWID();
GO
SET SHOWPLAN_TEXT OFF;
GO
DECLARE @day DATE = '2026-06-20';
SELECT b.book_id, b.title
FROM dbo.books AS b
WHERE b.is_active = 1
ORDER BY b.book_id
OFFSET ABS(CHECKSUM(@day)) % (SELECT COUNT(*) FROM dbo.books WHERE is_active = 1) ROWS FETCH NEXT 1 ROWS ONLY;
GO
DROP TABLE IF EXISTS dbo.guid_test;
CREATE TABLE dbo.guid_test (
    id_random     UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    id_sequential UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    n INT NOT NULL
);
INSERT INTO dbo.guid_test (n) VALUES (1), (2), (3);
SELECT n, LEFT(CAST(id_random AS CHAR(36)), 18) AS random_prefix, LEFT(CAST(id_sequential AS CHAR(36)), 18) AS sequential_prefix
FROM dbo.guid_test
ORDER BY n;
GO
DROP TABLE IF EXISTS dbo.order_audit;
CREATE TABLE dbo.order_audit (
    audit_id   INT IDENTITY(5000, 1) PRIMARY KEY,
    order_id   INT          NOT NULL,
    action     VARCHAR(10)  NOT NULL,
    audited_at DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME()
);
GO
CREATE OR ALTER TRIGGER dbo.tr_orders_audit ON dbo.orders AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.order_audit (order_id, action) SELECT order_id, 'insert' FROM inserted;
END;
GO
BEGIN TRAN;
INSERT INTO dbo.orders (customer_id, ordered_at, status) VALUES (1, '2026-06-30 10:00', 'Pending');
SELECT SCOPE_IDENTITY() AS scope_identity, @@IDENTITY AS at_at_identity, IDENT_CURRENT('dbo.orders') AS ident_current_orders;
ROLLBACK;
GO
BEGIN TRAN;
INSERT INTO dbo.orders (customer_id, ordered_at, status)
OUTPUT inserted.order_id, inserted.status
VALUES (1, '2026-06-30 10:00', 'Pending'), (3, '2026-06-30 10:05', 'Pending');
ROLLBACK;
GO
DECLARE @new TABLE (order_id INT, customer_id INT);
BEGIN TRAN;
INSERT INTO dbo.orders (customer_id, ordered_at, status)
OUTPUT inserted.order_id, inserted.customer_id INTO @new (order_id, customer_id)
VALUES (1, '2026-06-30 10:00', 'Pending'), (3, '2026-06-30 10:05', 'Pending');
SELECT order_id, customer_id FROM @new ORDER BY order_id;
SELECT @@TRANCOUNT AS open_transactions;
ROLLBACK;
SELECT @@TRANCOUNT AS after_rollback, IDENT_CURRENT('dbo.orders') AS ident_after_rollback;
GO
UPDATE dbo.books SET list_price = list_price WHERE category_id = 4;
IF @@ROWCOUNT = 0 PRINT 'nothing updated';
SELECT @@ROWCOUNT AS rowcount_after_if;
GO
BEGIN TRY
    SELECT 1 / 0;
END TRY
BEGIN CATCH
    SELECT ERROR_NUMBER() AS err_no, ERROR_SEVERITY() AS severity, ERROR_LINE() AS err_line, ERROR_MESSAGE() AS err_message;
END CATCH;
SELECT ERROR_NUMBER() AS outside_catch;
GO
SELECT DB_NAME() AS db_name, SUSER_SNAME() AS login_name, USER_NAME() AS user_name, APP_NAME() AS app_name;
GO
SELECT OBJECT_NAME(OBJECT_ID('dbo.books', 'U')) AS as_table, OBJECT_ID('dbo.books', 'V') AS as_view,
       OBJECT_ID('dbo.no_such_table') AS missing, COL_LENGTH('dbo.books', 'title') AS title_bytes,
       COL_LENGTH('dbo.books', 'isbn') AS isbn_bytes, COL_LENGTH('dbo.books', 'no_such_column') AS missing_column;
GO
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'books' AND COLUMN_NAME IN ('title', 'isbn', 'list_price')
ORDER BY ORDINAL_POSITION;
GO
SELECT i.name AS index_name, i.type_desc, i.is_unique, i.is_primary_key,
       STRING_AGG(c.name, ', ') WITHIN GROUP (ORDER BY ic.key_ordinal) AS key_columns
FROM sys.indexes AS i
JOIN sys.index_columns AS ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns AS c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.object_id = OBJECT_ID('dbo.order_items') AND ic.is_included_column = 0
GROUP BY i.name, i.type_desc, i.is_unique, i.is_primary_key
ORDER BY i.name;
GO
SELECT OBJECT_NAME(fk.parent_object_id) AS table_name, fk.name AS fk_name, c.name AS fk_column,
       OBJECT_NAME(fk.referenced_object_id) AS referenced_table
FROM sys.foreign_keys AS fk
JOIN sys.foreign_key_columns AS fkc ON fkc.constraint_object_id = fk.object_id
JOIN sys.columns AS c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
WHERE NOT EXISTS (
    SELECT 1
    FROM sys.index_columns AS ic
    WHERE ic.object_id = fkc.parent_object_id
      AND ic.column_id = fkc.parent_column_id
      AND ic.key_ordinal = 1
)
ORDER BY table_name, fk_name;
