CREATE TABLE dbo.Units
(
    Id            int IDENTITY(1, 1) PRIMARY KEY,
    InstitutionId int           NOT NULL,
    ParentId      int           NULL REFERENCES dbo.Units (Id),
    Name          nvarchar(200) NOT NULL
);

/* Kurum başına tek kök: ParentId'si NULL olan en fazla bir satır */
CREATE UNIQUE INDEX UX_Units_SingleRoot
    ON dbo.Units (InstitutionId)
    WHERE ParentId IS NULL;

INSERT dbo.Units (InstitutionId, ParentId, Name) VALUES (1, NULL, N'Sample School');
INSERT dbo.Units (InstitutionId, ParentId, Name) VALUES (1, 1, N'Administration');
INSERT dbo.Units (InstitutionId, ParentId, Name) VALUES (1, NULL, N'High School');
