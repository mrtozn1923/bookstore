-- Bookstore catalog tables used by the Elasticsearch series (SQL Server is the source of truth).
IF OBJECT_ID(N'dbo.BookAuthors') IS NOT NULL DROP TABLE dbo.BookAuthors;
IF OBJECT_ID(N'dbo.Books') IS NOT NULL DROP TABLE dbo.Books;
GO
CREATE TABLE dbo.Books
(
    BookId        int IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Books PRIMARY KEY,
    Isbn          char(13)       NOT NULL CONSTRAINT UQ_Books_Isbn UNIQUE,
    Title         nvarchar(300)  COLLATE Latin1_General_100_CI_AS NOT NULL,
    Language      char(2)        NOT NULL,
    Categories    nvarchar(200)  NOT NULL,
    Price         decimal(10, 2) NOT NULL,
    StockQuantity int            NOT NULL,
    PublishedOn   date           NOT NULL,
    AverageRating decimal(3, 2)  NULL,
    Description   nvarchar(2000) COLLATE Latin1_General_100_CI_AS NOT NULL
);
CREATE INDEX IX_Books_Title ON dbo.Books (Title);
GO
CREATE TABLE dbo.BookAuthors
(
    BookId   int           NOT NULL CONSTRAINT FK_BookAuthors_Books REFERENCES dbo.Books (BookId),
    Position tinyint       NOT NULL,
    Name     nvarchar(150) NOT NULL,
    Role     varchar(20)   NOT NULL,
    CONSTRAINT PK_BookAuthors PRIMARY KEY (BookId, Position)
);
GO
