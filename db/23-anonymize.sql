/* Run on the restored copy sqls_23_anon, never on production. */
UPDATE dbo.Customers
SET FirstName = CONCAT(N'Customer', CustomerId),
    LastName = N'Test',
    Email = CONCAT('customer', CustomerId, '@example.test'),
    City = N'-';
SELECT TOP (2) CustomerId, FirstName, LastName, Email, City FROM dbo.Customers ORDER BY CustomerId;
GO
/* After the BACPAC import: */
SELECT d.name, CAST(SUM(f.size) * 8 / 1024 AS int) AS data_mb, COUNT(*) AS files,
       (SELECT COUNT(*) FROM sqls_23_dev.dbo.Orders) AS orders,
       (SELECT TOP (1) Email FROM sqls_23_dev.dbo.Customers ORDER BY CustomerId) AS first_email
FROM sys.databases AS d JOIN sys.master_files AS f ON f.database_id = d.database_id AND f.type = 0
WHERE d.name = 'sqls_23_dev'
GROUP BY d.name;
GO
