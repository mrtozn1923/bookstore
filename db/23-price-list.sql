/* Post 23 - price list import into sqls_23. Copy the CSV files and prices.fmt to /var/opt/mssql/import/ in the SQL Server container first (see ../data). */
CREATE TABLE dbo.PriceListStaging
(
    Isbn nvarchar(20) NULL,
    Title nvarchar(200) NULL,
    ListPrice nvarchar(20) NULL,
    Currency nvarchar(10) NULL,
    ValidFrom nvarchar(20) NULL
);

GO
TRUNCATE TABLE dbo.PriceListStaging;
BULK INSERT dbo.PriceListStaging
FROM '/var/opt/mssql/import/prices_v1.csv'
WITH (FORMAT = 'CSV', FIRSTROW = 2, MAXERRORS = 100,
      ERRORFILE = '/var/opt/mssql/import/prices_v1_errors.txt');
GO
SELECT COUNT(*) AS rows_loaded FROM dbo.PriceListStaging;

GO
BULK INSERT dbo.PriceListStaging
FROM 'C:\Users\user\Downloads\prices.csv'
WITH (FORMAT = 'CSV', FIRSTROW = 2);

GO
TRUNCATE TABLE dbo.PriceListStaging;
BULK INSERT dbo.PriceListStaging
FROM '/var/opt/mssql/import/prices.csv'
WITH (FORMAT = 'CSV', FIRSTROW = 2, CODEPAGE = '65001');

GO
TRUNCATE TABLE dbo.PriceListStaging;
BULK INSERT dbo.PriceListStaging
FROM '/var/opt/mssql/import/prices_excel.csv'
WITH (FORMAT = 'CSV', FIRSTROW = 2, ROWTERMINATOR = '0x0a');
SELECT TOP (2) Isbn, Title, ValidFrom, LEN(ValidFrom) AS len,
       CAST(RIGHT(ValidFrom, 1) AS varbinary(2)) AS last_char, TRY_CONVERT(date, ValidFrom) AS valid_from
FROM dbo.PriceListStaging WHERE Isbn IN ('9780000000001', '9780000000002');

GO
TRUNCATE TABLE dbo.PriceListStaging;
BULK INSERT dbo.PriceListStaging
FROM '/var/opt/mssql/import/prices_excel.csv'
WITH (FORMAT = 'CSV', FIRSTROW = 2, ROWTERMINATOR = '0x0d0a');
SELECT TOP (2) Isbn, Title, ValidFrom, LEN(ValidFrom) AS len, TRY_CONVERT(date, ValidFrom) AS valid_from
FROM dbo.PriceListStaging WHERE Isbn IN ('9780000000001', '9780000000002');

GO
SELECT TOP (5) p.Isbn, p.Title, b.Price AS current_price, p.ListPrice AS new_price
FROM OPENROWSET(BULK '/var/opt/mssql/import/prices.csv',
                FORMAT = 'CSV', FIRSTROW = 2,
                FORMATFILE = '/var/opt/mssql/import/prices.fmt') AS p
JOIN dbo.Books AS b ON b.Isbn = p.Isbn
ORDER BY TRY_CONVERT(decimal(10,2), p.ListPrice) - b.Price DESC;

GO
TRUNCATE TABLE dbo.PriceListStaging;
BULK INSERT dbo.PriceListStaging
FROM '/var/opt/mssql/import/prices.csv'
WITH (FORMAT = 'CSV', FIRSTROW = 2, ROWTERMINATOR = '0x0a', TABLOCK);

GO
SELECT
    COUNT(*) AS total_rows,
    SUM(CASE WHEN NULLIF(s.Isbn, '') IS NULL THEN 1 ELSE 0 END) AS missing_isbn,
    SUM(CASE WHEN TRY_CONVERT(decimal(10,2), s.ListPrice) IS NULL THEN 1 ELSE 0 END) AS bad_price,
    SUM(CASE WHEN TRY_CONVERT(date, s.ValidFrom) IS NULL THEN 1 ELSE 0 END) AS bad_date,
    SUM(CASE WHEN b.BookId IS NOT NULL THEN 1 ELSE 0 END) AS known_books,
    SUM(CASE WHEN b.BookId IS NULL AND NULLIF(s.Isbn, '') IS NOT NULL THEN 1 ELSE 0 END) AS new_titles
FROM dbo.PriceListStaging AS s
LEFT JOIN dbo.Books AS b ON b.Isbn = s.Isbn;
SELECT TOP (3) s.Isbn, s.Title, s.ListPrice
FROM dbo.PriceListStaging AS s
WHERE TRY_CONVERT(decimal(10,2), s.ListPrice) IS NULL;

GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;
UPDATE b
SET b.Price = TRY_CONVERT(decimal(10,2), s.ListPrice)
FROM dbo.Books AS b
JOIN dbo.PriceListStaging AS s ON s.Isbn = b.Isbn
WHERE TRY_CONVERT(decimal(10,2), s.ListPrice) > 0
  AND s.Currency = 'TRY'
  AND b.Price <> TRY_CONVERT(decimal(10,2), s.ListPrice);
SELECT @@ROWCOUNT AS prices_updated;
COMMIT;

GO
SET NOCOUNT ON;
ALTER DATABASE sqls_23 SET RECOVERY FULL;
BACKUP DATABASE sqls_23 TO DISK = '/var/opt/mssql/backup/sqls_23_full.bak' WITH COMPRESSION, INIT;
DECLARE @r TABLE (recovery_model nvarchar(20), options varchar(20), log_mb decimal(8,1));
DECLARE @i int = 1, @rm nvarchar(20), @tl bit;
WHILE @i <= 4
BEGIN
    SELECT @rm = CASE WHEN @i <= 2 THEN N'FULL' ELSE N'BULK_LOGGED' END, @tl = @i % 2;
    IF @i = 3 ALTER DATABASE sqls_23 SET RECOVERY BULK_LOGGED;
    TRUNCATE TABLE dbo.PriceListStaging;
    BACKUP LOG sqls_23 TO DISK = '/var/opt/mssql/backup/sqls_23_log.trn' WITH INIT;
    IF @tl = 1
        BULK INSERT dbo.PriceListStaging FROM '/var/opt/mssql/import/prices.csv'
        WITH (FORMAT = 'CSV', FIRSTROW = 2, ROWTERMINATOR = '0x0a', TABLOCK);
    ELSE
        BULK INSERT dbo.PriceListStaging FROM '/var/opt/mssql/import/prices.csv'
        WITH (FORMAT = 'CSV', FIRSTROW = 2, ROWTERMINATOR = '0x0a');
    INSERT @r SELECT @rm, CASE WHEN @tl = 1 THEN 'TABLOCK' ELSE '-' END, log_since_last_log_backup_mb FROM sys.dm_db_log_stats(DB_ID());
    SET @i += 1;
END;
ALTER DATABASE sqls_23 SET RECOVERY SIMPLE;
SELECT * FROM @r;
GO
CREATE TABLE dbo.ClassicsCopy (Isbn varchar(13) NOT NULL, Title nvarchar(200) NOT NULL, Price decimal(10,2) NOT NULL);
GO
