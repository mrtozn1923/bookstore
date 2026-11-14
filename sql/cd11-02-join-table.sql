CREATE TABLE dbo.JInvoices (Id int PRIMARY KEY, InvoiceNumber varchar(16) NOT NULL);
CREATE TABLE dbo.Files (Id int IDENTITY PRIMARY KEY, FileName nvarchar(200) NOT NULL);
GO
CREATE TABLE dbo.InvoiceFiles
(
    InvoiceId int NOT NULL REFERENCES dbo.JInvoices (Id) ON DELETE CASCADE,
    FileId    int NOT NULL REFERENCES dbo.Files (Id),
    CONSTRAINT PK_InvoiceFiles PRIMARY KEY (InvoiceId, FileId)
);
INSERT dbo.JInvoices VALUES (1, 'INV-2026-0042');
INSERT dbo.Files (FileName) VALUES (N'inv-0042.pdf');
INSERT dbo.InvoiceFiles VALUES (1, 1);
DELETE dbo.JInvoices WHERE Id = 1;
SELECT (SELECT COUNT(*) FROM dbo.InvoiceFiles) AS LinkRows, (SELECT COUNT(*) FROM dbo.Files) AS FileRows;
