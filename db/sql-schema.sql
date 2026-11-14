CREATE TABLE dbo.Books (
    BookId    int NOT NULL CONSTRAINT PK_Books PRIMARY KEY,
    Isbn      char(13) NOT NULL CONSTRAINT UQ_Books_Isbn UNIQUE,
    Title     nvarchar(200) NOT NULL,
    Category  varchar(30) NOT NULL,
    Price     decimal(10,2) NOT NULL
);

CREATE TABLE dbo.Customers (
    CustomerId int NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
    Email      varchar(200) NOT NULL CONSTRAINT UQ_Customers_Email UNIQUE,
    FullName   nvarchar(200) NOT NULL,
    Country    char(2) NOT NULL,
    CreatedAt  datetime2(0) NOT NULL
);

CREATE TABLE dbo.Orders (
    OrderId     bigint IDENTITY NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
    CustomerId  int NOT NULL CONSTRAINT FK_Orders_Customers REFERENCES dbo.Customers,
    Status      varchar(20) NOT NULL,
    TotalAmount decimal(12,2) NOT NULL,
    Currency    char(3) NOT NULL CONSTRAINT DF_Orders_Currency DEFAULT 'TRY',
    CreatedAt   datetime2(0) NOT NULL
);

CREATE TABLE dbo.OrderItems (
    OrderId   bigint NOT NULL CONSTRAINT FK_OrderItems_Orders REFERENCES dbo.Orders,
    LineNumber    smallint NOT NULL,
    BookId    int NOT NULL CONSTRAINT FK_OrderItems_Books REFERENCES dbo.Books,
    Quantity  int NOT NULL,
    UnitPrice decimal(10,2) NOT NULL,
    CONSTRAINT PK_OrderItems PRIMARY KEY (OrderId, LineNumber)
);
