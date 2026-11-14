DECLARE @boundaries nvarchar(max) = (
    SELECT STRING_AGG(CONCAT('''', CONVERT(char(10), DATEADD(MONTH, value, '2022-11-01'), 23), ''''), ', ')
           WITHIN GROUP (ORDER BY value)
    FROM GENERATE_SERIES(0, 50));
EXEC (N'CREATE PARTITION FUNCTION pf_orders_month (datetime2(0)) AS RANGE RIGHT FOR VALUES (' + @boundaries + N')');
GO
CREATE PARTITION SCHEME ps_orders_month AS PARTITION pf_orders_month ALL TO ([PRIMARY]);
GO
CREATE TABLE dbo.OrdersP (
    OrderId     bigint IDENTITY NOT NULL,
    CustomerId  int NOT NULL,
    Status      varchar(20) NOT NULL,
    TotalAmount decimal(12,2) NOT NULL,
    Currency    char(3) NOT NULL CONSTRAINT DF_OrdersP_Currency DEFAULT 'TRY',
    CreatedAt   datetime2(0) NOT NULL,
    CONSTRAINT PK_OrdersP PRIMARY KEY CLUSTERED (OrderId, CreatedAt)
) ON ps_orders_month (CreatedAt);

CREATE TABLE dbo.OrderItemsP (
    OrderId    bigint NOT NULL,
    LineNumber smallint NOT NULL,
    CreatedAt  datetime2(0) NOT NULL,
    BookId     int NOT NULL,
    Quantity   int NOT NULL,
    UnitPrice  decimal(10,2) NOT NULL,
    CONSTRAINT PK_OrderItemsP PRIMARY KEY CLUSTERED (OrderId, LineNumber, CreatedAt)
) ON ps_orders_month (CreatedAt);

CREATE INDEX IX_OrdersP_CustomerId_CreatedAt ON dbo.OrdersP (CustomerId, CreatedAt DESC)
    INCLUDE (Status, TotalAmount) ON ps_orders_month (CreatedAt);
GO
