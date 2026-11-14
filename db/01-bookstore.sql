-- BookstoreDb schema and sample catalog (idempotent: safe to run more than once)
IF DB_ID(N'BookstoreDb') IS NULL
    CREATE DATABASE BookstoreDb;
GO

USE BookstoreDb;
GO

IF OBJECT_ID(N'dbo.Books', N'U') IS NULL
    CREATE TABLE dbo.Books
    (
        Isbn     char(13)      NOT NULL CONSTRAINT PK_Books PRIMARY KEY,
        Title    nvarchar(200) NOT NULL,
        Author   nvarchar(200) NOT NULL,
        PriceTry decimal(10,2) NOT NULL
    );
GO

MERGE dbo.Books AS target
USING (VALUES
    ('9780441172719', N'Dune', N'Frank Herbert', 420.00),
    ('9789750719387', N'İnce Memed', N'Yaşar Kemal', 310.00),
    ('9789750738609', N'Çalıkuşu', N'Reşat Nuri Güntekin', 240.00),
    ('9789754700114', N'Saatleri Ayarlama Enstitüsü', N'Ahmet Hamdi Tanpınar', 280.00),
    ('9780141439587', N'Emma', N'Jane Austen', 180.00),
    ('9789750722752', N'Kürk Mantolu Madonna', N'Sabahattin Ali', 150.00)
) AS source (Isbn, Title, Author, PriceTry)
ON target.Isbn = source.Isbn
WHEN NOT MATCHED THEN
    INSERT (Isbn, Title, Author, PriceTry) VALUES (source.Isbn, source.Title, source.Author, source.PriceTry);
GO
