-- Post 10: materialized path
CREATE TABLE dbo.UnitPathModel
(
    Id   int           NOT NULL PRIMARY KEY,
    Path varchar(400)  NOT NULL UNIQUE,
    Name nvarchar(100) NOT NULL
);
INSERT dbo.UnitPathModel (Id, Path, Name) VALUES
    (1, '/1/', N'Head Office'), (2, '/1/2/', N'Purchasing'), (3, '/1/3/', N'Accounting'),
    (4, '/1/2/4/', N'Book Orders'), (5, '/1/2/5/', N'Receiving'), (6, '/1/3/6/', N'Payments');
GO
SELECT Id, Path, Name FROM dbo.UnitPathModel WHERE Path LIKE '/1/2/%' ORDER BY Path;
DECLARE @old varchar(400) = '/1/2/5/', @new varchar(400) = '/1/3/5/';
UPDATE dbo.UnitPathModel
SET Path = @new + SUBSTRING(Path, LEN(@old) + 1, 400)
WHERE Path LIKE @old + '%';
SELECT Id, Path, Name FROM dbo.UnitPathModel ORDER BY Path;
