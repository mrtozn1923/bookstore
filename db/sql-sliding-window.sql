CREATE OR ALTER PROCEDURE dbo.SlideOrdersWindow
    @keepFrom datetime2(0),
    @openUntil datetime2(0)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @oldest datetime2(0), @newest datetime2(0), @t datetime2 = SYSDATETIME(), @orders int, @switchMs int;

    SELECT @newest = CAST(MAX(v.value) AS datetime2(0))
    FROM sys.partition_range_values AS v
    JOIN sys.partition_functions AS f ON f.function_id = v.function_id
    WHERE f.name = N'pf_orders_month';
    WHILE @newest < @openUntil
    BEGIN
        SET @newest = DATEADD(MONTH, 1, @newest);
        ALTER PARTITION SCHEME ps_orders_month NEXT USED [PRIMARY];
        ALTER PARTITION FUNCTION pf_orders_month() SPLIT RANGE (@newest);
        PRINT CONCAT('opened partition from ', CONVERT(char(10), @newest, 23));
    END;

    WHILE 1 = 1
    BEGIN
        SELECT @oldest = CAST(MIN(v.value) AS datetime2(0))
        FROM sys.partition_range_values AS v
        JOIN sys.partition_functions AS f ON f.function_id = v.function_id
        WHERE f.name = N'pf_orders_month';
        IF DATEADD(MONTH, 1, @oldest) > @keepFrom BREAK;

        SET @t = SYSDATETIME();
        BEGIN TRANSACTION;
        ALTER TABLE dbo.OrderItemsP SWITCH PARTITION 2 TO dbo.OrderItemsArchiveStage
            WITH (WAIT_AT_LOW_PRIORITY (MAX_DURATION = 1 MINUTES, ABORT_AFTER_WAIT = SELF));
        ALTER TABLE dbo.OrdersP SWITCH PARTITION 2 TO dbo.OrdersArchiveStage
            WITH (WAIT_AT_LOW_PRIORITY (MAX_DURATION = 1 MINUTES, ABORT_AFTER_WAIT = SELF));
        ALTER PARTITION FUNCTION pf_orders_month() MERGE RANGE (@oldest);
        COMMIT;
        SET @switchMs = DATEDIFF(MILLISECOND, @t, SYSDATETIME());

        BEGIN TRANSACTION;
        INSERT INTO dbo.OrdersArchive (OrderId, CustomerId, Status, TotalAmount, Currency, CreatedAt)
        SELECT OrderId, CustomerId, Status, TotalAmount, Currency, CreatedAt FROM dbo.OrdersArchiveStage;
        SET @orders = @@ROWCOUNT;
        INSERT INTO dbo.OrderItemsArchive (OrderId, LineNumber, BookId, Quantity, UnitPrice)
        SELECT OrderId, LineNumber, BookId, Quantity, UnitPrice FROM dbo.OrderItemsArchiveStage;
        TRUNCATE TABLE dbo.OrdersArchiveStage;
        TRUNCATE TABLE dbo.OrderItemsArchiveStage;
        COMMIT;

        PRINT CONCAT('archived ', CONVERT(char(7), @oldest, 23), ': ', @orders, ' orders, switch+merge ', @switchMs,
                     ' ms, copy ', DATEDIFF(MILLISECOND, @t, SYSDATETIME()) - @switchMs, ' ms');
    END;
END;
GO
