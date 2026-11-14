CREATE TABLE dbo.InstitutionUsersArchive
(
    Id        int           NOT NULL PRIMARY KEY,
    FullName  nvarchar(200) NOT NULL,
    Email     nvarchar(200) NOT NULL,
    JoinedOn  date          NOT NULL,
    DeletedAt datetime2     NOT NULL
);

DELETE FROM dbo.InstitutionUsers
OUTPUT deleted.Id, deleted.FullName, deleted.Email, deleted.JoinedOn, SYSUTCDATETIME()
    INTO dbo.InstitutionUsersArchive (Id, FullName, Email, JoinedOn, DeletedAt)
WHERE Id = 1;

SELECT Id, FullName, Email, JoinedOn FROM dbo.InstitutionUsersArchive;
SELECT COUNT(*) AS LiveRows FROM dbo.InstitutionUsers;
