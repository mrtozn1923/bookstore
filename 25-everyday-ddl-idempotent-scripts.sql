ALTER TABLE dbo.customers ADD loyalty_tier VARCHAR(10) NULL;
CREATE UNIQUE INDEX UX_customers_phone ON dbo.customers (phone);
GO
ALTER TABLE dbo.customers ADD loyalty_tier VARCHAR(10) NULL;
CREATE UNIQUE INDEX UX_customers_phone ON dbo.customers (phone);
GO
SELECT COL_LENGTH('dbo.customers', 'loyalty_tier') AS loyalty_tier_bytes,
       INDEXPROPERTY(OBJECT_ID('dbo.customers'), 'UX_customers_phone', 'IndexId') AS ux_index_id;
GO
SELECT IDENT_SEED('dbo.orders') AS seed, IDENT_INCR('dbo.orders') AS increment, IDENT_CURRENT('dbo.orders') AS current_value;
BEGIN TRAN;
INSERT INTO dbo.orders (customer_id, ordered_at, status) VALUES (1, '2026-06-30 10:00', 'Pending');
ROLLBACK;
INSERT INTO dbo.orders (customer_id, ordered_at, status) VALUES (1, '2026-06-30 10:00', 'Unknown');
INSERT INTO dbo.orders (customer_id, ordered_at, status) VALUES (1, '2026-06-30 10:00', 'Pending');
SELECT TOP (2) order_id, status FROM dbo.orders ORDER BY order_id DESC;
GO
INSERT INTO dbo.orders (order_id, customer_id, ordered_at, status) VALUES (381, 1, '2026-06-30 10:00', 'Pending');
GO
SET IDENTITY_INSERT dbo.orders ON;
INSERT INTO dbo.orders (order_id, customer_id, ordered_at, status) VALUES (381, 1, '2026-06-30 10:05', 'Pending');
SET IDENTITY_INSERT dbo.orders OFF;
INSERT INTO dbo.orders (customer_id, ordered_at, status) VALUES (1, '2026-06-30 10:10', 'Pending');
SELECT TOP (3) order_id, ordered_at FROM dbo.orders ORDER BY order_id DESC;
GO
DELETE FROM dbo.orders WHERE order_id > 379;
DBCC CHECKIDENT ('dbo.orders', RESEED, 379);
INSERT INTO dbo.orders (customer_id, ordered_at, status) VALUES (1, '2026-06-30 10:15', 'Pending');
SELECT TOP (1) order_id FROM dbo.orders ORDER BY order_id DESC;
GO
IF OBJECT_ID('dbo.order_number_seq', 'SO') IS NULL
    CREATE SEQUENCE dbo.order_number_seq AS INT START WITH 100001 INCREMENT BY 1 CACHE 50;
IF COL_LENGTH('dbo.orders', 'order_number') IS NULL
    ALTER TABLE dbo.orders ADD order_number INT NULL
        CONSTRAINT DF_orders_order_number DEFAULT (NEXT VALUE FOR dbo.order_number_seq);
DROP TABLE IF EXISTS dbo.institution_orders;
CREATE TABLE dbo.institution_orders (
    institution_order_id INT IDENTITY(1,1) CONSTRAINT PK_institution_orders PRIMARY KEY,
    institution_name     NVARCHAR(100) NOT NULL,
    order_number         INT NOT NULL CONSTRAINT DF_institution_orders_order_number DEFAULT (NEXT VALUE FOR dbo.order_number_seq)
);
GO
INSERT INTO dbo.orders (customer_id, ordered_at, status) VALUES (1, '2026-06-30 11:00', 'Pending'), (3, '2026-06-30 11:05', 'Pending');
INSERT INTO dbo.institution_orders (institution_name) VALUES (N'Hilltop School Library');
SELECT 'orders' AS source, order_id AS id, order_number FROM dbo.orders WHERE order_number IS NOT NULL
UNION ALL
SELECT 'institution_orders', institution_order_id, order_number FROM dbo.institution_orders
ORDER BY order_number;
GO
SELECT NEXT VALUE FOR dbo.order_number_seq AS peek_consumes;
DECLARE @first SQL_VARIANT, @last SQL_VARIANT;
EXEC sys.sp_sequence_get_range @sequence_name = N'dbo.order_number_seq', @range_size = 100,
     @range_first_value = @first OUTPUT, @range_last_value = @last OUTPUT;
SELECT @first AS range_first, @last AS range_last;
SELECT current_value, cache_size, is_cached FROM sys.sequences WHERE name = 'order_number_seq';
GO
IF COL_LENGTH('dbo.order_items', 'line_total') IS NULL
    ALTER TABLE dbo.order_items ADD line_total AS (quantity * unit_price) PERSISTED;
GO
SELECT TOP (3) order_item_id, quantity, unit_price, line_total FROM dbo.order_items WHERE quantity > 1 ORDER BY order_item_id;
SELECT c.name, c.is_computed, cc.is_persisted, TYPE_NAME(c.system_type_id) AS type_name, c.precision, c.scale
FROM sys.columns AS c
LEFT JOIN sys.computed_columns AS cc ON cc.object_id = c.object_id AND cc.column_id = c.column_id
WHERE c.object_id = OBJECT_ID('dbo.order_items') AND c.name = 'line_total';
GO
ALTER TABLE dbo.orders ADD age_days AS DATEDIFF(day, ordered_at, SYSUTCDATETIME()) PERSISTED;
GO
ALTER TABLE dbo.customers DROP COLUMN IF EXISTS loyalty_tier;
GO
SET XACT_ABORT ON;
BEGIN TRAN;
ALTER TABLE dbo.customers ADD loyalty_tier VARCHAR(10) NULL;
CREATE UNIQUE INDEX UX_customers_phone ON dbo.customers (phone);
COMMIT;
GO
SELECT COL_LENGTH('dbo.customers', 'loyalty_tier') AS loyalty_tier_bytes, @@TRANCOUNT AS open_tran;
GO
SET XACT_ABORT ON;
BEGIN TRAN;
IF COL_LENGTH('dbo.customers', 'loyalty_tier') IS NULL
    ALTER TABLE dbo.customers ADD loyalty_tier VARCHAR(10) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.customers') AND name = 'UX_customers_phone')
    CREATE UNIQUE INDEX UX_customers_phone ON dbo.customers (phone) WHERE phone IS NOT NULL;
COMMIT;
PRINT 'migration applied';
GO 2
SELECT COL_LENGTH('dbo.customers', 'loyalty_tier') AS loyalty_tier_bytes,
       INDEXPROPERTY(OBJECT_ID('dbo.customers'), 'UX_customers_phone', 'IndexId') AS ux_index_id;
GO
IF COL_LENGTH('dbo.orders', 'order_code') IS NULL
    ALTER TABLE dbo.orders ADD order_code AS CONCAT('BK-', order_number);
SELECT TOP (2) order_id, order_number, order_code FROM dbo.orders WHERE order_number IS NOT NULL ORDER BY order_id;
GO
IF COL_LENGTH('dbo.orders', 'order_code') IS NULL
    ALTER TABLE dbo.orders ADD order_code AS CONCAT('BK-', order_number);
GO
SELECT TOP (2) order_id, order_number, order_code FROM dbo.orders WHERE order_number IS NOT NULL ORDER BY order_id;
GO
CREATE OR ALTER VIEW dbo.v_book_prices AS
SELECT book_id, title, list_price FROM dbo.books WHERE is_active = 1;
GO
CREATE OR ALTER VIEW dbo.v_book_prices AS
SELECT book_id, title, list_price, listed_on FROM dbo.books WHERE is_active = 1;
GO
SELECT TOP (2) * FROM dbo.v_book_prices ORDER BY book_id;
GO
ALTER TABLE dbo.customers DROP COLUMN IF EXISTS no_such_column;
ALTER TABLE dbo.customers DROP CONSTRAINT IF EXISTS no_such_constraint;
DROP INDEX IF EXISTS no_such_index ON dbo.customers;
DROP SEQUENCE IF EXISTS dbo.no_such_seq;
DROP SYNONYM IF EXISTS dbo.no_such_synonym;
PRINT 'nothing to drop, no error';
GO
SELECT referencing_schema_name, referencing_entity_name FROM sys.dm_sql_referencing_entities('dbo.books', 'OBJECT');
GO
EXEC sp_rename 'dbo.books.listed_on', 'listed_date', 'COLUMN';
GO
SELECT TOP (2) * FROM dbo.v_book_prices ORDER BY book_id;
GO
EXEC sp_rename 'dbo.books.listed_date', 'listed_on', 'COLUMN';
GO
SELECT TOP (1) book_id, listed_on FROM dbo.v_book_prices ORDER BY book_id;
GO
CREATE OR ALTER VIEW dbo.v_book_prices WITH SCHEMABINDING AS
SELECT book_id, title, list_price, listed_on FROM dbo.books WHERE is_active = 1;
GO
EXEC sp_rename 'dbo.books.listed_on', 'listed_date', 'COLUMN';
GO
CREATE OR ALTER SYNONYM dbo.catalog_books FOR dbo.books;
GO
DROP SYNONYM IF EXISTS dbo.catalog_books;
CREATE SYNONYM dbo.catalog_books FOR dbo.books;
SELECT COUNT(*) AS via_synonym FROM dbo.catalog_books;
SELECT name, base_object_name FROM sys.synonyms;
