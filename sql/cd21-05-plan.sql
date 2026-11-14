-- Post 21: the OR keyset condition becomes two seek ranges on the index
SET SHOWPLAN_TEXT ON;
GO
DECLARE @c datetime2(0) = '2024-01-02T17:40:10', @i int = 1500102;
SELECT TOP (20) Id, CreatedAt, TotalAmount FROM dbo.Orders
WHERE InstitutionId = 7 AND (CreatedAt < @c OR (CreatedAt = @c AND Id < @i))
ORDER BY CreatedAt DESC, Id DESC;
GO
SET SHOWPLAN_TEXT OFF;
GO
