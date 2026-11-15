/* Post 20 - Backup and restore. Run with sqlcmd in SQLCMD mode (-I), after creating sqls_20 and loading SCHEMA.sql:
   sqlcmd -S localhost -U sa -C -I -d master -Q "CREATE DATABASE sqls_20"
   sqlcmd -S localhost -U sa -C -I -d sqls_20 -i SCHEMA.sql
   sqlcmd -S localhost -U sa -C -I -i 20-backup-restore.sql
   The backup folder /var/opt/mssql/backup must exist (Linux container) - on Windows change the paths.
   Set StopAt below to the last_good_moment value printed by the scenario. */
:setvar StopAt "2026-11-14T23:46:47.93"
USE sqls_20;
GO
CREATE OR ALTER VIEW dbo.LogState
AS
SELECT d.recovery_model_desc, d.log_reuse_wait_desc,
       CAST(u.used_log_space_in_percent AS decimal(5,1)) AS log_used_pct,
       CAST(u.total_log_size_in_bytes / 1048576 AS int) AS log_file_mb
FROM sys.databases AS d
CROSS JOIN sys.dm_db_log_space_usage AS u
WHERE d.database_id = DB_ID();
GO
ALTER DATABASE sqls_20 SET RECOVERY FULL;
UPDATE dbo.Payments SET Amount = Amount + 0.01 WHERE PaymentId <= 300000;
CHECKPOINT;
SELECT '0. FULL, no backup yet' AS step, * FROM dbo.LogState;

BACKUP DATABASE sqls_20 TO DISK = '/var/opt/mssql/backup/sqls_20_full_a.bak' WITH CHECKSUM, COMPRESSION, INIT;
SELECT '1. full backup' AS step, * FROM dbo.LogState;

UPDATE dbo.Payments SET Amount = Amount - 0.01 WHERE PaymentId <= 300000;
UPDATE dbo.Payments SET Amount = Amount + 0.01 WHERE PaymentId <= 300000;
CHECKPOINT;
SELECT '2. 600k row updates' AS step, * FROM dbo.LogState;

BACKUP DATABASE sqls_20 TO DISK = '/var/opt/mssql/backup/sqls_20_full_b.bak' WITH CHECKSUM, COMPRESSION, INIT;
CHECKPOINT;
SELECT '3. second full backup' AS step, * FROM dbo.LogState;

BACKUP LOG sqls_20 TO DISK = '/var/opt/mssql/backup/sqls_20_log_b.trn' WITH CHECKSUM, COMPRESSION, INIT;
SELECT '4. log backup' AS step, * FROM dbo.LogState;
GO
BACKUP DATABASE sqls_20 TO DISK = '/var/opt/mssql/backup/sqls_20_full_weekly.bak' WITH CHECKSUM, COMPRESSION, INIT;
UPDATE dbo.StockItems SET QuantityOnHand = QuantityOnHand + 1 WHERE BookId <= 1000;
BACKUP DATABASE sqls_20 TO DISK = '/var/opt/mssql/backup/sqls_20_for_dev.bak' WITH CHECKSUM, COMPRESSION, INIT;
UPDATE dbo.StockItems SET QuantityOnHand = QuantityOnHand - 1 WHERE BookId <= 1000;
BACKUP DATABASE sqls_20 TO DISK = '/var/opt/mssql/backup/sqls_20_diff_nightly.bak' WITH DIFFERENTIAL, CHECKSUM, COMPRESSION, INIT;
GO
USE master;
GO
RESTORE DATABASE sqls_20_test FROM DISK = '/var/opt/mssql/backup/sqls_20_full_weekly.bak'
WITH MOVE 'sqls_20' TO '/var/opt/mssql/data/sqls_20_test.mdf',
     MOVE 'sqls_20_log' TO '/var/opt/mssql/data/sqls_20_test_log.ldf',
     NORECOVERY;
RESTORE DATABASE sqls_20_test FROM DISK = '/var/opt/mssql/backup/sqls_20_diff_nightly.bak' WITH RECOVERY;
GO
SELECT RIGHT(m.physical_device_name, CHARINDEX('/', REVERSE(m.physical_device_name)) - 1) AS file_name,
       b.type, b.is_copy_only, b.first_lsn, b.last_lsn, b.differential_base_lsn
FROM msdb.dbo.backupset AS b
JOIN msdb.dbo.backupmediafamily AS m ON m.media_set_id = b.media_set_id
WHERE b.database_name = 'sqls_20'
  AND b.backup_start_date >= DATEADD(MINUTE, -10, SYSDATETIME())
ORDER BY b.backup_set_id;
GO
BACKUP DATABASE sqls_20 TO DISK = '/var/opt/mssql/backup/sqls_20_c0.bak' WITH COPY_ONLY, CHECKSUM, NO_COMPRESSION, INIT;
BACKUP DATABASE sqls_20 TO DISK = '/var/opt/mssql/backup/sqls_20_c1.bak' WITH COPY_ONLY, CHECKSUM, COMPRESSION, INIT;
BACKUP DATABASE sqls_20 TO DISK = '/var/opt/mssql/backup/sqls_20_c2.bak' WITH COPY_ONLY, CHECKSUM, COMPRESSION (ALGORITHM = ZSTD), INIT;
BACKUP DATABASE sqls_20 TO DISK = '/var/opt/mssql/backup/sqls_20_c3.bak' WITH COPY_ONLY, CHECKSUM, COMPRESSION (ALGORITHM = ZSTD, LEVEL = HIGH), INIT;
GO
SELECT RIGHT(m.physical_device_name, 14) AS file_name, b.compression_algorithm,
       CAST(b.backup_size / 1048576 AS decimal(8,1)) AS backup_mb,
       CAST(b.compressed_backup_size / 1048576 AS decimal(8,1)) AS on_disk_mb,
       CAST(b.backup_size / b.compressed_backup_size AS decimal(4,2)) AS ratio
FROM msdb.dbo.backupset AS b
JOIN msdb.dbo.backupmediafamily AS m ON m.media_set_id = b.media_set_id
WHERE b.database_name = 'sqls_20' AND m.physical_device_name LIKE '%sqls_20_c_.bak'
ORDER BY m.physical_device_name;
GO
/* 08: copy sqls_20_c1.bak to sqls_20_damaged.bak and overwrite a few bytes before running the second line */
RESTORE VERIFYONLY FROM DISK = '/var/opt/mssql/backup/sqls_20_c1.bak' WITH CHECKSUM;
RESTORE VERIFYONLY FROM DISK = '/var/opt/mssql/backup/sqls_20_damaged.bak' WITH CHECKSUM;
GO
USE sqls_20;
GO
BACKUP DATABASE sqls_20 TO DISK = '/var/opt/mssql/backup/sqls_20_full.bak' WITH CHECKSUM, COMPRESSION, INIT;
WAITFOR DELAY '00:00:03';
INSERT dbo.Orders (OrderNumber, CustomerId, Status, CreatedAt, TotalAmount, Currency)
VALUES ('BK-01000001', 42, 'Pending', SYSUTCDATETIME(), 0, 'TRY');
BACKUP DATABASE sqls_20 TO DISK = '/var/opt/mssql/backup/sqls_20_diff.bak' WITH DIFFERENTIAL, CHECKSUM, COMPRESSION, INIT;
WAITFOR DELAY '00:00:03';
INSERT dbo.Orders (OrderNumber, CustomerId, Status, CreatedAt, TotalAmount, Currency)
VALUES ('BK-01000002', 43, 'Pending', SYSUTCDATETIME(), 0, 'TRY');
BACKUP LOG sqls_20 TO DISK = '/var/opt/mssql/backup/sqls_20_log_1.trn' WITH CHECKSUM, COMPRESSION, INIT;
WAITFOR DELAY '00:00:03';
INSERT dbo.Orders (OrderNumber, CustomerId, Status, CreatedAt, TotalAmount, Currency)
VALUES ('BK-01000003', 44, 'Pending', SYSUTCDATETIME(), 0, 'TRY');
WAITFOR DELAY '00:00:02';
SELECT SYSUTCDATETIME() AS last_good_moment;
WAITFOR DELAY '00:00:02';
GO
BEGIN TRANSACTION PendingCleanup WITH MARK 'pending order cleanup';
DELETE i FROM dbo.OrderItems AS i JOIN dbo.Orders AS o ON o.OrderId = i.OrderId
WHERE o.Status = 'Pending' AND o.CreatedAt >= '2026-10-20';
DELETE p FROM dbo.Payments AS p JOIN dbo.Orders AS o ON o.OrderId = p.OrderId
WHERE o.Status = 'Pending' AND o.CreatedAt >= '2026-10-20';
DELETE FROM dbo.Orders WHERE Status = 'Pending' AND CreatedAt >= '2026-10-20';
SELECT @@ROWCOUNT AS deleted_orders, SYSUTCDATETIME() AS deleted_at;
COMMIT;
GO
WAITFOR DELAY '00:00:03';
INSERT dbo.Orders (OrderNumber, CustomerId, Status, CreatedAt, TotalAmount, Currency)
VALUES ('BK-01000004', 45, 'Pending', SYSUTCDATETIME(), 0, 'TRY');
BACKUP LOG sqls_20 TO DISK = '/var/opt/mssql/backup/sqls_20_log_2.trn' WITH CHECKSUM, COMPRESSION, INIT;
GO
USE master;
GO
RESTORE DATABASE sqls_20_pitr FROM DISK = '/var/opt/mssql/backup/sqls_20_full.bak'
WITH MOVE 'sqls_20' TO '/var/opt/mssql/data/sqls_20_pitr.mdf',
     MOVE 'sqls_20_log' TO '/var/opt/mssql/data/sqls_20_pitr_log.ldf';
RESTORE DATABASE sqls_20_pitr FROM DISK = '/var/opt/mssql/backup/sqls_20_diff.bak' WITH NORECOVERY;
GO
RESTORE DATABASE sqls_20_pitr FROM DISK = '/var/opt/mssql/backup/sqls_20_full.bak'
WITH MOVE 'sqls_20' TO '/var/opt/mssql/data/sqls_20_pitr.mdf',
     MOVE 'sqls_20_log' TO '/var/opt/mssql/data/sqls_20_pitr_log.ldf',
     NORECOVERY, REPLACE;
RESTORE DATABASE sqls_20_pitr FROM DISK = '/var/opt/mssql/backup/sqls_20_diff.bak' WITH NORECOVERY;
RESTORE LOG sqls_20_pitr FROM DISK = '/var/opt/mssql/backup/sqls_20_log_2.trn' WITH NORECOVERY;
GO
RESTORE LOG sqls_20_pitr FROM DISK = '/var/opt/mssql/backup/sqls_20_log_1.trn' WITH NORECOVERY;
RESTORE LOG sqls_20_pitr FROM DISK = '/var/opt/mssql/backup/sqls_20_log_2.trn'
WITH STOPAT = '$(StopAt)', RECOVERY;
GO
SELECT 'production' AS db, COUNT(*) AS orders_since_1020, MAX(OrderNumber) AS last_order
FROM sqls_20.dbo.Orders WHERE CreatedAt >= '2026-10-20'
UNION ALL
SELECT 'restored copy', COUNT(*), MAX(OrderNumber)
FROM sqls_20_pitr.dbo.Orders WHERE CreatedAt >= '2026-10-20';
GO
USE sqls_20;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
SET IDENTITY_INSERT dbo.Orders ON;
INSERT dbo.Orders (OrderId, OrderNumber, CustomerId, Status, CreatedAt, TotalAmount, Currency)
SELECT r.OrderId, r.OrderNumber, r.CustomerId, r.Status, r.CreatedAt, r.TotalAmount, r.Currency
FROM sqls_20_pitr.dbo.Orders AS r
WHERE r.CreatedAt >= '2026-10-20'
  AND NOT EXISTS (SELECT 1 FROM dbo.Orders AS o WHERE o.OrderId = r.OrderId);
PRINT CONCAT('orders: ', @@ROWCOUNT);
SET IDENTITY_INSERT dbo.Orders OFF;
SET IDENTITY_INSERT dbo.OrderItems ON;
INSERT dbo.OrderItems (OrderItemId, OrderId, BookId, Quantity, UnitPrice)
SELECT r.OrderItemId, r.OrderId, r.BookId, r.Quantity, r.UnitPrice
FROM sqls_20_pitr.dbo.OrderItems AS r
WHERE r.OrderId IN (SELECT OrderId FROM sqls_20_pitr.dbo.Orders WHERE CreatedAt >= '2026-10-20')
  AND NOT EXISTS (SELECT 1 FROM dbo.OrderItems AS i WHERE i.OrderItemId = r.OrderItemId);
PRINT CONCAT('order items: ', @@ROWCOUNT);
SET IDENTITY_INSERT dbo.OrderItems OFF;
SET IDENTITY_INSERT dbo.Payments ON;
INSERT dbo.Payments (PaymentId, OrderId, Amount, Currency, Method, Status, CreatedAt)
SELECT r.PaymentId, r.OrderId, r.Amount, r.Currency, r.Method, r.Status, r.CreatedAt
FROM sqls_20_pitr.dbo.Payments AS r
WHERE r.OrderId IN (SELECT OrderId FROM sqls_20_pitr.dbo.Orders WHERE CreatedAt >= '2026-10-20')
  AND NOT EXISTS (SELECT 1 FROM dbo.Payments AS p WHERE p.PaymentId = r.PaymentId);
PRINT CONCAT('payments: ', @@ROWCOUNT);
SET IDENTITY_INSERT dbo.Payments OFF;
COMMIT;
GO
USE master;
GO
RESTORE DATABASE sqls_20_mark FROM DISK = '/var/opt/mssql/backup/sqls_20_full.bak'
WITH MOVE 'sqls_20' TO '/var/opt/mssql/data/sqls_20_mark.mdf',
     MOVE 'sqls_20_log' TO '/var/opt/mssql/data/sqls_20_mark_log.ldf',
     NORECOVERY;
RESTORE DATABASE sqls_20_mark FROM DISK = '/var/opt/mssql/backup/sqls_20_diff.bak' WITH NORECOVERY;
RESTORE LOG sqls_20_mark FROM DISK = '/var/opt/mssql/backup/sqls_20_log_1.trn' WITH NORECOVERY;
RESTORE LOG sqls_20_mark FROM DISK = '/var/opt/mssql/backup/sqls_20_log_2.trn'
WITH STOPBEFOREMARK = 'PendingCleanup', RECOVERY;
SELECT COUNT(*) AS orders_since_1020 FROM sqls_20_mark.dbo.Orders WHERE CreatedAt >= '2026-10-20';
SELECT mark_name, description, mark_time FROM msdb.dbo.logmarkhistory WHERE database_name = 'sqls_20';
GO
USE sqls_20;
GO
ALTER DATABASE sqls_20 MODIFY FILE (NAME = 'sqls_20_log', MAXSIZE = 400MB);
DECLARE @i int = 1, @used int, @total int;
WHILE @i <= 20
BEGIN
    UPDATE dbo.Payments SET Amount = Amount + 0.01 WHERE PaymentId <= 300000;
    SELECT @used = used_log_space_in_bytes / 1048576, @total = total_log_size_in_bytes / 1048576
    FROM sys.dm_db_log_space_usage;
    RAISERROR('round %d: log used %d MB of %d MB', 0, 1, @i, @used, @total) WITH NOWAIT;
    SET @i += 1;
END;
GO
SELECT log_reuse_wait_desc, log_used_pct, log_file_mb FROM dbo.LogState;
DBCC SHRINKFILE ('sqls_20_log', 64);
GO
BACKUP LOG sqls_20 TO DISK = '/var/opt/mssql/backup/sqls_20_log_3.trn' WITH CHECKSUM, COMPRESSION, INIT;
SELECT log_reuse_wait_desc, log_used_pct, log_file_mb FROM dbo.LogState;
GO
USE master;
GO
SET NOCOUNT ON;
ALTER DATABASE sqls_20 SET TRUSTWORTHY ON;
BACKUP DATABASE sqls_20 TO DISK = '/var/opt/mssql/backup/sqls_20_vendor.bak' WITH COPY_ONLY, CHECKSUM, COMPRESSION, INIT;
ALTER DATABASE sqls_20 SET TRUSTWORTHY OFF;
RESTORE DATABASE sqls_20_vendor FROM DISK = '/var/opt/mssql/backup/sqls_20_vendor.bak'
WITH MOVE 'sqls_20' TO '/var/opt/mssql/data/sqls_20_vendor.mdf',
     MOVE 'sqls_20_log' TO '/var/opt/mssql/data/sqls_20_vendor_log.ldf',
     RESTRICTED_USER;
SELECT name, is_trustworthy_on, user_access_desc, SUSER_SNAME(owner_sid) AS owner
FROM sys.databases WHERE name = 'sqls_20_vendor';
GO
