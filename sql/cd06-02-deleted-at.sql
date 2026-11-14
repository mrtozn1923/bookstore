CREATE TABLE dbo.InstitutionUsers
(
    Id            int IDENTITY PRIMARY KEY,
    InstitutionId int           NOT NULL,
    Email         nvarchar(200) NOT NULL,
    FullName      nvarchar(200) NOT NULL,
    DeletedAt     datetime2(0)  NULL,
    DeletedBy     nvarchar(100) NULL
);
CREATE UNIQUE INDEX UX_InstitutionUsers_Email_Active
    ON dbo.InstitutionUsers (InstitutionId, Email)
    WHERE DeletedAt IS NULL;

INSERT dbo.InstitutionUsers (InstitutionId, Email, FullName) VALUES (1, N'ada@example.com', N'Ada Lovelace');
UPDATE dbo.InstitutionUsers SET DeletedAt = '2026-03-31', DeletedBy = N'grace' WHERE Email = N'ada@example.com';
INSERT dbo.InstitutionUsers (InstitutionId, Email, FullName) VALUES (1, N'ada@example.com', N'Ada Lovelace');
SELECT Id, Email, DeletedAt FROM dbo.InstitutionUsers;
