DECLARE @boundaries nvarchar(max) = (
    SELECT STRING_AGG(CONCAT('''', CONVERT(char(10), DATEADD(MONTH, value, '2022-11-01'), 23), ''''), ', ')
           WITHIN GROUP (ORDER BY value)
    FROM GENERATE_SERIES(0, 50));
EXEC (N'CREATE PARTITION FUNCTION pf_orders_month7 (datetime2(7)) AS RANGE RIGHT FOR VALUES (' + @boundaries + N')');
GO
CREATE PARTITION SCHEME ps_orders_month7 AS PARTITION pf_orders_month7 ALL TO ([PRIMARY]);
GO
CREATE TABLE dbo.OrdersP7 (
    OrderId     bigint NOT NULL,
    CustomerId  int NOT NULL,
    TotalAmount decimal(12,2) NOT NULL,
    CreatedAt   datetime2(7) NOT NULL,
    CONSTRAINT PK_OrdersP7 PRIMARY KEY CLUSTERED (OrderId, CreatedAt)
) ON ps_orders_month7 (CreatedAt);
INSERT INTO dbo.OrdersP7 WITH (TABLOCK) (OrderId, CustomerId, TotalAmount, CreatedAt)
SELECT OrderId, CustomerId, TotalAmount, CreatedAt FROM dbo.Orders;
GO
