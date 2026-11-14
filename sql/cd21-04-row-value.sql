SELECT TOP (20) Id FROM dbo.Orders
WHERE InstitutionId = 7 AND (CreatedAt, Id) < ('2024-01-02T17:40:10', 1500102)
ORDER BY CreatedAt DESC, Id DESC;
GO
