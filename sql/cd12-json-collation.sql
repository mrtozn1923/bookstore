-- Post 12: what collation does JSON_VALUE return, and does CREATE JSON INDEX work? (SQL Server 2025, compatibility level 170)
CREATE TABLE dbo.InstitutionUnits (Id int IDENTITY PRIMARY KEY, Name json NOT NULL);
INSERT dbo.InstitutionUnits (Name) VALUES (N'{"Tr":"İdari İşler","En":null}'), (N'{"Tr":"Işıklı Sınıf","En":null}'), (N'{"Tr":"Kütüphane","En":"Library"}');
SELECT SQL_VARIANT_PROPERTY(CAST(JSON_VALUE(Name, '$.Tr' RETURNING nvarchar(100)) AS sql_variant), 'Collation') AS Coll FROM dbo.InstitutionUnits WHERE Id = 1;
SELECT STRING_AGG(n, ', ') WITHIN GROUP (ORDER BY n) AS DefaultOrder FROM (SELECT JSON_VALUE(Name, '$.Tr' RETURNING nvarchar(100)) AS n FROM dbo.InstitutionUnits) x;
SELECT STRING_AGG(n, ', ') WITHIN GROUP (ORDER BY n COLLATE Turkish_100_CI_AS) AS TurkishOrder FROM (SELECT JSON_VALUE(Name, '$.Tr' RETURNING nvarchar(100)) AS n FROM dbo.InstitutionUnits) x;
GO
CREATE JSON INDEX IX_InstitutionUnits_Name_Json ON dbo.InstitutionUnits (Name) FOR ('$.En', '$.Tr');
GO
SELECT name, type_desc FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.InstitutionUnits');
