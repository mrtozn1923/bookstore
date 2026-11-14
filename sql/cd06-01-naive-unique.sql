CREATE TABLE dbo.InstitutionUsers
(
    Id            int IDENTITY PRIMARY KEY,
    InstitutionId int           NOT NULL,
    Email         nvarchar(200) NOT NULL,
    FullName      nvarchar(200) NOT NULL,
    IsDeleted     bit           NOT NULL DEFAULT 0
);
CREATE UNIQUE INDEX UX_InstitutionUsers_Email ON dbo.InstitutionUsers (InstitutionId, Email);

INSERT dbo.InstitutionUsers (InstitutionId, Email, FullName) VALUES (1, N'ada@example.com', N'Ada Lovelace');
UPDATE dbo.InstitutionUsers SET IsDeleted = 1 WHERE Email = N'ada@example.com';
INSERT dbo.InstitutionUsers (InstitutionId, Email, FullName) VALUES (1, N'ada@example.com', N'Ada Lovelace');
