-- Post 10: closure table next to the adjacency list
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
CREATE TABLE dbo.InstitutionUnitPaths
(
    AncestorId   int NOT NULL REFERENCES dbo.InstitutionUnits (Id),
    DescendantId int NOT NULL REFERENCES dbo.InstitutionUnits (Id),
    Depth        int NOT NULL,
    CONSTRAINT PK_InstitutionUnitPaths PRIMARY KEY (AncestorId, DescendantId)
);
CREATE INDEX IX_InstitutionUnitPaths_Descendant ON dbo.InstitutionUnitPaths (DescendantId, AncestorId);
GO
-- fill from the adjacency list (every node with itself and with each ancestor)
WITH Paths AS
(
    SELECT Id AS AncestorId, Id AS DescendantId, 0 AS Depth FROM dbo.InstitutionUnits
    UNION ALL
    SELECT p.AncestorId, u.Id, p.Depth + 1
    FROM Paths AS p
    JOIN dbo.InstitutionUnits AS u ON u.ParentId = p.DescendantId
)
INSERT dbo.InstitutionUnitPaths (AncestorId, DescendantId, Depth)
SELECT AncestorId, DescendantId, Depth FROM Paths;
SELECT COUNT(*) AS PathRows FROM dbo.InstitutionUnitPaths;
GO
SELECT u.Id, u.Name, p.Depth
FROM dbo.InstitutionUnitPaths AS p
JOIN dbo.InstitutionUnits AS u ON u.Id = p.DescendantId
WHERE p.AncestorId = 2
ORDER BY p.Depth, u.Id;
GO
DECLARE @node int = 5, @newParent int = 3;
BEGIN TRANSACTION;
DELETE p
FROM dbo.InstitutionUnitPaths AS p
WHERE p.DescendantId IN (SELECT DescendantId FROM dbo.InstitutionUnitPaths WHERE AncestorId = @node)
  AND p.AncestorId NOT IN (SELECT DescendantId FROM dbo.InstitutionUnitPaths WHERE AncestorId = @node);
INSERT dbo.InstitutionUnitPaths (AncestorId, DescendantId, Depth)
SELECT super.AncestorId, sub.DescendantId, super.Depth + sub.Depth + 1
FROM dbo.InstitutionUnitPaths AS super
CROSS JOIN dbo.InstitutionUnitPaths AS sub
WHERE super.DescendantId = @newParent
  AND sub.AncestorId = @node;
UPDATE dbo.InstitutionUnits SET ParentId = @newParent WHERE Id = @node;
COMMIT;
SELECT AncestorId, Depth FROM dbo.InstitutionUnitPaths WHERE DescendantId = 5 ORDER BY Depth;
