UPDATE dbo.InstitutionUsers
SET IsDeleted = 1,
    Email = CONCAT(Email, N'#deleted-', Id)
WHERE Id = 1;

SELECT Id, Email, IsDeleted FROM dbo.InstitutionUsers;
