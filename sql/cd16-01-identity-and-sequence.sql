-- Run in an empty database: identity and sequence values are consumed outside the transaction
SET NOCOUNT ON;
CREATE TABLE dbo.OrdersDemo (Id int IDENTITY(1,1) PRIMARY KEY, Note nvarchar(50) NOT NULL);

INSERT dbo.OrdersDemo (Note) VALUES (N'first');
BEGIN TRANSACTION;
    INSERT dbo.OrdersDemo (Note) VALUES (N'rolled back');
ROLLBACK;
INSERT dbo.OrdersDemo (Note) VALUES (N'third');
SELECT Id, Note FROM dbo.OrdersDemo;
GO
CREATE SEQUENCE dbo.InvoiceSeqDemo AS int START WITH 1 INCREMENT BY 1 CACHE 50;
GO
DECLARE @a int = NEXT VALUE FOR dbo.InvoiceSeqDemo;
BEGIN TRANSACTION;
    DECLARE @b int = NEXT VALUE FOR dbo.InvoiceSeqDemo;
ROLLBACK;
DECLARE @c int = NEXT VALUE FOR dbo.InvoiceSeqDemo;
SELECT @a AS First, @b AS RolledBack, @c AS Next;
GO
