CREATE TABLE dbo.OrdersArchiveStage (
    OrderId     bigint NOT NULL,
    CustomerId  int NOT NULL,
    Status      varchar(20) NOT NULL,
    TotalAmount decimal(12,2) NOT NULL,
    Currency    char(3) NOT NULL,
    CreatedAt   datetime2(0) NOT NULL,
    CONSTRAINT PK_OrdersArchiveStage PRIMARY KEY CLUSTERED (OrderId, CreatedAt)
) ON [PRIMARY];
CREATE INDEX IX_OrdersArchiveStage_CustomerId_CreatedAt ON dbo.OrdersArchiveStage (CustomerId, CreatedAt DESC)
    INCLUDE (Status, TotalAmount) ON [PRIMARY];

CREATE TABLE dbo.OrderItemsArchiveStage (
    OrderId    bigint NOT NULL,
    LineNumber smallint NOT NULL,
    CreatedAt  datetime2(0) NOT NULL,
    BookId     int NOT NULL,
    Quantity   int NOT NULL,
    UnitPrice  decimal(10,2) NOT NULL,
    CONSTRAINT PK_OrderItemsArchiveStage PRIMARY KEY CLUSTERED (OrderId, LineNumber, CreatedAt)
) ON [PRIMARY];
GO
