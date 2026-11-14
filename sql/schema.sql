-- Bookstore schema for the Node.js API; safe to run more than once
IF OBJECT_ID(N'dbo.Authors', N'U') IS NULL
CREATE TABLE dbo.Authors (
    Id int IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Authors PRIMARY KEY,
    Name nvarchar(200) NOT NULL CONSTRAINT UQ_Authors_Name UNIQUE
);
GO
IF OBJECT_ID(N'dbo.Books', N'U') IS NULL
CREATE TABLE dbo.Books (
    Isbn char(13) NOT NULL CONSTRAINT PK_Books PRIMARY KEY,
    Title nvarchar(200) NOT NULL,
    AuthorId int NOT NULL CONSTRAINT FK_Books_Authors REFERENCES dbo.Authors (Id),
    -- Money as decimal(10,2) here; the API converts it to integer minor units (34990)
    Price decimal(10, 2) NOT NULL CONSTRAINT CK_Books_Price CHECK (Price > 0),
    Currency char(3) NOT NULL CONSTRAINT CK_Books_Currency CHECK (Currency IN ('TRY', 'EUR'))
);
GO
IF OBJECT_ID(N'dbo.StockItems', N'U') IS NULL
CREATE TABLE dbo.StockItems (
    Isbn char(13) NOT NULL CONSTRAINT PK_StockItems PRIMARY KEY CONSTRAINT FK_StockItems_Books REFERENCES dbo.Books (Isbn),
    Quantity int NOT NULL CONSTRAINT CK_StockItems_Quantity CHECK (Quantity >= 0)
);
GO
IF OBJECT_ID(N'dbo.OrderNumbers', N'SO') IS NULL
CREATE SEQUENCE dbo.OrderNumbers AS int START WITH 1001;
GO
IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
CREATE TABLE dbo.Orders (
    Id int NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
    CustomerId nvarchar(64) NOT NULL,
    Status varchar(20) NOT NULL,
    Currency char(3) NOT NULL,
    CreatedAtUtc datetime2(3) NOT NULL
);
GO
IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NULL
CREATE TABLE dbo.OrderItems (
    OrderId int NOT NULL CONSTRAINT FK_OrderItems_Orders REFERENCES dbo.Orders (Id),
    LineNumber int NOT NULL,
    Isbn char(13) NOT NULL CONSTRAINT FK_OrderItems_Books REFERENCES dbo.Books (Isbn),
    Quantity int NOT NULL CONSTRAINT CK_OrderItems_Quantity CHECK (Quantity > 0),
    UnitPrice decimal(10, 2) NOT NULL,
    CONSTRAINT PK_OrderItems PRIMARY KEY (OrderId, LineNumber)
);
GO
-- Inserts or updates one book and its author; new books start with @InitialStock copies
CREATE OR ALTER PROCEDURE dbo.UpsertBook
    @Isbn char(13),
    @Title nvarchar(200),
    @Author nvarchar(200),
    @Price decimal(10, 2),
    @Currency char(3),
    @InitialStock int,
    @Inserted bit OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @AuthorId int = (SELECT Id FROM dbo.Authors WHERE Name = @Author);
    IF @AuthorId IS NULL
    BEGIN
        INSERT dbo.Authors (Name) VALUES (@Author);
        SET @AuthorId = SCOPE_IDENTITY();
    END;

    UPDATE dbo.Books SET Title = @Title, AuthorId = @AuthorId, Price = @Price, Currency = @Currency WHERE Isbn = @Isbn;
    SET @Inserted = IIF(@@ROWCOUNT = 0, 1, 0);
    IF @Inserted = 1
    BEGIN
        INSERT dbo.Books (Isbn, Title, AuthorId, Price, Currency) VALUES (@Isbn, @Title, @AuthorId, @Price, @Currency);
        INSERT dbo.StockItems (Isbn, Quantity) VALUES (@Isbn, @InitialStock);
    END;
END;
GO
