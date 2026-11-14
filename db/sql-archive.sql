CREATE TABLE dbo.OrdersArchive (
    OrderId     bigint NOT NULL CONSTRAINT PK_OrdersArchive PRIMARY KEY,
    CustomerId  int NOT NULL,
    Status      varchar(20) NOT NULL,
    TotalAmount decimal(12,2) NOT NULL,
    Currency    char(3) NOT NULL,
    CreatedAt   datetime2(0) NOT NULL
);

CREATE TABLE dbo.OrderItemsArchive (
    OrderId    bigint NOT NULL,
    LineNumber smallint NOT NULL,
    BookId     int NOT NULL,
    Quantity   int NOT NULL,
    UnitPrice  decimal(10,2) NOT NULL,
    CONSTRAINT PK_OrderItemsArchive PRIMARY KEY (OrderId, LineNumber)
);

CREATE TABLE dbo.ArchiveProgress (
    JobName     varchar(50) NOT NULL CONSTRAINT PK_ArchiveProgress PRIMARY KEY,
    LastOrderId bigint NOT NULL,
    MovedOrders bigint NOT NULL,
    UpdatedAt   datetime2(0) NOT NULL
);
