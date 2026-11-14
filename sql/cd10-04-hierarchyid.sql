-- Post 10: SQL Server hierarchyid
CREATE TABLE dbo.UnitHid
(
    Node  hierarchyid   NOT NULL PRIMARY KEY,
    Level AS Node.GetLevel() PERSISTED,
    Id    int           NOT NULL UNIQUE,
    Name  nvarchar(100) NOT NULL
);
INSERT dbo.UnitHid (Node, Id, Name) VALUES
    ('/1/', 1, N'Head Office'), ('/1/1/', 2, N'Purchasing'), ('/1/2/', 3, N'Accounting'),
    ('/1/1/1/', 4, N'Book Orders'), ('/1/1/2/', 5, N'Receiving'), ('/1/2/1/', 6, N'Payments');
GO
DECLARE @purchasing hierarchyid = (SELECT Node FROM dbo.UnitHid WHERE Id = 2);
SELECT Node.ToString() AS Node, Level, Id, Name FROM dbo.UnitHid WHERE Node.IsDescendantOf(@purchasing) = 1 ORDER BY Node;
DECLARE @old hierarchyid = (SELECT Node FROM dbo.UnitHid WHERE Id = 5);
DECLARE @accounting hierarchyid = (SELECT Node FROM dbo.UnitHid WHERE Id = 3);
DECLARE @lastChild hierarchyid = (SELECT MAX(Node) FROM dbo.UnitHid WHERE Node.GetAncestor(1) = @accounting);
DECLARE @new hierarchyid = @accounting.GetDescendant(@lastChild, NULL);
UPDATE dbo.UnitHid SET Node = Node.GetReparentedValue(@old, @new) WHERE Node.IsDescendantOf(@old) = 1;
SELECT Node.ToString() AS Node, Level, Id, Name FROM dbo.UnitHid ORDER BY Node;
