-- Post 21: offset vs keyset vs COUNT(*) (run before and after cd21-03-index.sql)
DECLARE @c datetime2(0), @i int;
SELECT @c = CreatedAt, @i = Id FROM dbo.Orders WHERE InstitutionId = 7
ORDER BY CreatedAt DESC, Id DESC OFFSET 99979 ROWS FETCH NEXT 1 ROWS ONLY;

SET STATISTICS IO ON;
/* offset: page 1 */
SELECT Id, CreatedAt, TotalAmount FROM dbo.Orders WHERE InstitutionId = 7
ORDER BY CreatedAt DESC, Id DESC OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY;
/* offset: page 5000 */
SELECT Id, CreatedAt, TotalAmount FROM dbo.Orders WHERE InstitutionId = 7
ORDER BY CreatedAt DESC, Id DESC OFFSET 99980 ROWS FETCH NEXT 20 ROWS ONLY;
/* offset: page 20000 (last page) */
SELECT Id, CreatedAt, TotalAmount FROM dbo.Orders WHERE InstitutionId = 7
ORDER BY CreatedAt DESC, Id DESC OFFSET 399980 ROWS FETCH NEXT 20 ROWS ONLY;
/* keyset: after the last row of page 4999 */
SELECT TOP (20) Id, CreatedAt, TotalAmount FROM dbo.Orders
WHERE InstitutionId = 7 AND (CreatedAt < @c OR (CreatedAt = @c AND Id < @i))
ORDER BY CreatedAt DESC, Id DESC;
SELECT COUNT(*) FROM dbo.Orders WHERE InstitutionId = 7;
/* SELECT *, offset page 5000 */
SELECT * FROM dbo.Orders WHERE InstitutionId = 7
ORDER BY CreatedAt DESC, Id DESC OFFSET 99980 ROWS FETCH NEXT 20 ROWS ONLY;
/* SELECT *, keyset after page 4999 */
SELECT TOP (20) * FROM dbo.Orders
WHERE InstitutionId = 7 AND (CreatedAt < @c OR (CreatedAt = @c AND Id < @i))
ORDER BY CreatedAt DESC, Id DESC;
SET STATISTICS IO OFF;
GO
