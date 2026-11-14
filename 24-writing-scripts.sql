DECLARE @price_set DECIMAL(10,2) = 0, @price_select DECIMAL(10,2) = 0;
SET @price_set = (SELECT list_price FROM dbo.books WHERE book_id = 999);
SELECT @price_select = list_price FROM dbo.books WHERE book_id = 999;
SELECT @price_set AS with_set, @price_select AS with_select;
GO
DECLARE @title NVARCHAR(200);
SELECT @title = title FROM dbo.books WHERE category_id = 4;
DECLARE @n INT = @@ROWCOUNT;
SELECT @title AS last_title, @n AS rows_read;
SET @title = (SELECT title FROM dbo.books WHERE category_id = 4);
SELECT 'after SET' AS step, @title AS title;
GO
DECLARE @batch_size INT = 20;
SELECT @batch_size AS in_first_batch;
GO
SELECT @batch_size AS in_second_batch;
GO
SELECT COUNT(*) AS books FROM dbo.books;
SELEC 1;
GO
SELECT COUNT(*) AS books FROM dbo.books;
SELECT COUNT(*) FROM dbo.no_such_table;
SELECT 'still running' AS step;
GO
DROP VIEW IF EXISTS dbo.v_active_books;
CREATE VIEW dbo.v_active_books AS SELECT book_id, title FROM dbo.books WHERE is_active = 1;
GO
EXEC sp_executesql N'DROP VIEW IF EXISTS dbo.v_active_books;
GO
CREATE VIEW dbo.v_active_books AS SELECT book_id, title FROM dbo.books WHERE is_active = 1;';
GO
EXEC sp_executesql N'SELECT COUNT(*) FROM dbo.books
GO';
GO
SET NOCOUNT ON;
IF EXISTS (SELECT 1 FROM dbo.books AS b
           WHERE b.is_active = 1
             AND NOT EXISTS (SELECT 1 FROM dbo.stock_items AS s WHERE s.book_id = b.book_id))
BEGIN
    INSERT INTO dbo.stock_items (book_id, quantity_on_hand)
    SELECT b.book_id, 0
    FROM dbo.books AS b
    WHERE b.is_active = 1
      AND NOT EXISTS (SELECT 1 FROM dbo.stock_items AS s WHERE s.book_id = b.book_id);
    PRINT CONCAT(@@ROWCOUNT, ' stock rows created');
END
ELSE
    PRINT 'every active book has a stock row';
GO 2
GO
DECLARE @name SYSNAME, @sql NVARCHAR(400), @n INT;
DECLARE tables_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT name FROM sys.tables WHERE name IN ('books', 'order_items', 'orders') ORDER BY name;
OPEN tables_cursor;
FETCH NEXT FROM tables_cursor INTO @name;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = N'SELECT @n = COUNT(*) FROM dbo.' + QUOTENAME(@name) + N';';
    EXEC sp_executesql @sql, N'@n INT OUTPUT', @n = @n OUTPUT;
    PRINT CONCAT(@name, N': ', @n);
    FETCH NEXT FROM tables_cursor INTO @name;
END;
CLOSE tables_cursor;
DEALLOCATE tables_cursor;
GO
SELECT t.name AS table_name, SUM(p.rows) AS row_count
FROM sys.tables AS t
JOIN sys.partitions AS p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
WHERE t.name IN ('books', 'order_items', 'orders')
GROUP BY t.name
ORDER BY t.name;
GO
BEGIN TRY
    SELECT COUNT(*) AS c FROM dbo.no_such_table;
END TRY
BEGIN CATCH
    SELECT 'caught' AS step, ERROR_NUMBER() AS err_no;
END CATCH;
SELECT 'after' AS step;
GO
BEGIN TRY
    EXEC sp_executesql N'SELECT COUNT(*) AS c FROM dbo.no_such_table;';
END TRY
BEGIN CATCH
    SELECT 'caught' AS step, ERROR_NUMBER() AS err_no, ERROR_MESSAGE() AS msg;
END CATCH;
GO
BEGIN TRY
    RAISERROR('just a warning', 10, 1);
    SELECT 'after severity 10' AS step;
END TRY
BEGIN CATCH
    SELECT 'caught' AS step;
END CATCH;
GO
SET XACT_ABORT OFF;
BEGIN TRY
    BEGIN TRAN;
    UPDATE dbo.books SET list_price = list_price + 10 WHERE book_id = 26;
    UPDATE dbo.books SET list_price = -1 WHERE book_id = 28;
    COMMIT;
END TRY
BEGIN CATCH
    SELECT 'xact_abort off' AS mode, XACT_STATE() AS xact_state, @@TRANCOUNT AS trancount, ERROR_NUMBER() AS err_no;
    IF XACT_STATE() <> 0 ROLLBACK;
END CATCH;
GO
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRAN;
    UPDATE dbo.books SET list_price = list_price + 10 WHERE book_id = 26;
    UPDATE dbo.books SET list_price = -1 WHERE book_id = 28;
    COMMIT;
END TRY
BEGIN CATCH
    SELECT 'xact_abort on' AS mode, XACT_STATE() AS xact_state, @@TRANCOUNT AS trancount, ERROR_NUMBER() AS err_no;
    IF XACT_STATE() <> 0 ROLLBACK;
END CATCH;
SELECT list_price FROM dbo.books WHERE book_id = 26;
GO
BEGIN TRAN;
BEGIN TRY
    UPDATE dbo.books SET list_price = -1 WHERE book_id = 26;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK
    THROW;
END CATCH;
GO
DROP TABLE IF EXISTS dbo.price_updates;
CREATE TABLE dbo.price_updates (
    book_id   INT           NOT NULL CONSTRAINT PK_price_updates PRIMARY KEY,
    new_price DECIMAL(10,2) NOT NULL
);
INSERT INTO dbo.price_updates (book_id, new_price)
SELECT book_id, ROUND(list_price * 1.08, 0) FROM dbo.books;
UPDATE dbo.price_updates SET new_price = -1 WHERE book_id = 50;
SELECT COUNT(*) AS staged, SUM(CASE WHEN new_price < 0 THEN 1 ELSE 0 END) AS invalid FROM dbo.price_updates;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @batch_size INT = 20, @batch_no INT = 0, @from_id INT, @to_id INT = 0, @rows INT, @msg NVARCHAR(2048);
WHILE 1 = 1
BEGIN
    SET @from_id = @to_id + 1;
    SELECT @to_id = MAX(book_id)
    FROM (SELECT TOP (@batch_size) book_id FROM dbo.price_updates WHERE book_id >= @from_id ORDER BY book_id) AS next_batch;
    IF @to_id IS NULL BREAK;
    SET @batch_no += 1;
    BEGIN TRY
        BEGIN TRAN;
        UPDATE b
        SET list_price = p.new_price
        FROM dbo.books AS b
        JOIN dbo.price_updates AS p ON p.book_id = b.book_id
        WHERE b.book_id BETWEEN @from_id AND @to_id
          AND b.list_price <> p.new_price;
        SET @rows = @@ROWCOUNT;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        SET @msg = CONCAT(N'Batch ', @batch_no, N' (book_id ', @from_id, N'-', @to_id, N') failed: ', ERROR_MESSAGE());
        THROW 50010, @msg, 1;
    END CATCH;
    RAISERROR(N'Batch %d: book_id %d-%d, %d rows updated', 0, 1, @batch_no, @from_id, @to_id, @rows) WITH NOWAIT;
END;
GO
SELECT COUNT(*) AS updated FROM dbo.books AS b JOIN dbo.price_updates AS p ON p.book_id = b.book_id WHERE b.list_price = p.new_price;
GO
IF EXISTS (SELECT 1 FROM dbo.price_updates WHERE new_price <= 0)
BEGIN
    SELECT book_id, new_price FROM dbo.price_updates WHERE new_price <= 0;
    THROW 50011, N'price_updates contains non-positive prices; nothing was applied.', 1;
END;
PRINT N'staging data is valid';
GO
:on error exit
:setvar BatchSize 20
UPDATE dbo.price_updates SET new_price = 345.00 WHERE book_id = 50;
GO
:r price-update.sql
GO
SELECT COUNT(*) AS updated FROM dbo.books AS b JOIN dbo.price_updates AS p ON p.book_id = b.book_id WHERE b.list_price = p.new_price;
