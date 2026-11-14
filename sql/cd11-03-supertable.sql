CREATE TABLE dbo.Attachables (Id int IDENTITY PRIMARY KEY, OwnerType varchar(20) NOT NULL);
CREATE TABLE dbo.SInvoices
(
    Id            int IDENTITY PRIMARY KEY,
    AttachableId  int NOT NULL UNIQUE REFERENCES dbo.Attachables (Id),
    InvoiceNumber varchar(16) NOT NULL
);
CREATE TABLE dbo.SAttachments
(
    Id           int IDENTITY PRIMARY KEY,
    AttachableId int NOT NULL CONSTRAINT FK_SAttachments_Attachables REFERENCES dbo.Attachables (Id),
    FileName     nvarchar(200) NOT NULL
);
INSERT dbo.SAttachments (AttachableId, FileName) VALUES (99, N'ghost.pdf');
