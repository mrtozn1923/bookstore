CREATE TABLE dbo.NaiveInvoices         (Id int PRIMARY KEY, InvoiceNumber varchar(16) NOT NULL);
CREATE TABLE dbo.NaiveInstitutionUsers (Id int PRIMARY KEY, FullName nvarchar(100) NOT NULL);
CREATE TABLE dbo.NaiveAttachments
(
    Id         int IDENTITY PRIMARY KEY,
    EntityType varchar(50)   NOT NULL,
    EntityId   int           NOT NULL,
    FileName   nvarchar(200) NOT NULL
);
INSERT dbo.NaiveInvoices VALUES (1, 'INV-2026-0042');
INSERT dbo.NaiveInstitutionUsers VALUES (1, N'Ada Lovelace');
INSERT dbo.NaiveAttachments (EntityType, EntityId, FileName) VALUES
    ('Invoice', 1, N'inv-0042.pdf'),
    ('InstitutionUser', 1, N'authorization-letter.pdf'),
    ('invoice', 1, N'credit-note-118.pdf'),
    ('Invoice', 7, N'scan-0007.pdf');
DELETE dbo.NaiveInvoices WHERE Id = 1;
GO
SELECT a.Id, a.EntityType, a.EntityId, a.FileName
FROM dbo.NaiveAttachments AS a
WHERE NOT EXISTS (SELECT 1 FROM dbo.NaiveInvoices         AS i WHERE a.EntityType = 'Invoice'         AND i.Id = a.EntityId)
  AND NOT EXISTS (SELECT 1 FROM dbo.NaiveInstitutionUsers AS u WHERE a.EntityType = 'InstitutionUser' AND u.Id = a.EntityId);
