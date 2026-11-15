-- Bookstore tables used by the Go API. Safe to run more than once.
IF OBJECT_ID(N'dbo.Books', N'U') IS NULL
CREATE TABLE dbo.Books (
    Isbn   char(13)       NOT NULL CONSTRAINT PK_Books PRIMARY KEY,
    Title  nvarchar(200)  NOT NULL,
    Author nvarchar(200)  NOT NULL,
    Price  decimal(10, 2) NOT NULL,
    Stock  int            NOT NULL CONSTRAINT CK_Books_Stock CHECK (Stock >= 0)
);

IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
CREATE TABLE dbo.Orders (
    Id         varchar(20)  NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
    CustomerId nvarchar(64) NOT NULL,
    Status     tinyint      NOT NULL,
    CreatedAt  datetime2(3) NOT NULL,
    UpdatedAt  datetime2(3) NOT NULL,
    PaidAt     datetime2(3) NULL
);

IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NULL
CREATE TABLE dbo.OrderItems (
    OrderId    varchar(20)    NOT NULL CONSTRAINT FK_OrderItems_Orders REFERENCES dbo.Orders (Id),
    LineNumber int            NOT NULL,
    Isbn       char(13)       NOT NULL CONSTRAINT FK_OrderItems_Books REFERENCES dbo.Books (Isbn),
    Title      nvarchar(200)  NOT NULL,
    UnitPrice  decimal(10, 2) NOT NULL,
    Quantity   int            NOT NULL CONSTRAINT CK_OrderItems_Quantity CHECK (Quantity > 0),
    CONSTRAINT PK_OrderItems PRIMARY KEY (OrderId, LineNumber)
);

IF OBJECT_ID(N'dbo.OrderNumbers', N'SO') IS NULL
CREATE SEQUENCE dbo.OrderNumbers AS int START WITH 1001;
