-- Post 10: the six-node tree as an adjacency list; subtree with a recursive CTE
CREATE TABLE dbo.InstitutionUnits
(
    Id       int           NOT NULL PRIMARY KEY,
    ParentId int           NULL REFERENCES dbo.InstitutionUnits (Id),
    Name     nvarchar(100) NOT NULL
);
INSERT dbo.InstitutionUnits (Id, ParentId, Name) VALUES
    (1, NULL, N'Head Office'), (2, 1, N'Purchasing'), (3, 1, N'Accounting'),
    (4, 2, N'Book Orders'), (5, 2, N'Receiving'), (6, 3, N'Payments');
GO
WITH Subtree AS
(
    SELECT Id, Name, 0 AS Depth
    FROM dbo.InstitutionUnits
    WHERE Id = 2
    UNION ALL
    SELECT u.Id, u.Name, s.Depth + 1
    FROM dbo.InstitutionUnits AS u
    JOIN Subtree AS s ON u.ParentId = s.Id
)
SELECT Id, Name, Depth FROM Subtree ORDER BY Depth, Id;
GO
-- a faulty move: Purchasing under its own child Book Orders
UPDATE dbo.InstitutionUnits SET ParentId = 4 WHERE Id = 2;
GO
WITH Subtree AS
(
    SELECT Id, ParentId, 0 AS Depth FROM dbo.InstitutionUnits WHERE Id = 2
    UNION ALL
    SELECT u.Id, u.ParentId, s.Depth + 1
    FROM dbo.InstitutionUnits AS u
    JOIN Subtree AS s ON u.ParentId = s.Id
)
SELECT COUNT(*) AS Rows FROM Subtree;
GO
-- read-side guard: carry the visited path, never enter a node twice
WITH Subtree AS
(
    SELECT Id, 0 AS Depth, CAST('/' + CAST(Id AS varchar(10)) + '/' AS varchar(900)) AS Visited
    FROM dbo.InstitutionUnits WHERE Id = 2
    UNION ALL
    SELECT u.Id, s.Depth + 1, CAST(s.Visited + CAST(u.Id AS varchar(10)) + '/' AS varchar(900))
    FROM dbo.InstitutionUnits AS u
    JOIN Subtree AS s ON u.ParentId = s.Id
    WHERE s.Visited NOT LIKE '%/' + CAST(u.Id AS varchar(10)) + '/%'
)
SELECT Id, Depth, Visited FROM Subtree ORDER BY Depth, Id;
