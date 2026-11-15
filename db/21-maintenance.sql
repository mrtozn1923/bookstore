/* Post 21 - Maintenance. Create sqls_21, load SCHEMA.sql, then run this file with sqlcmd -I.
   Ola Hallengren's MaintenanceSolution.sql (https://ola.hallengren.com, MIT) must be installed into sqls_21
   right after the last RESTORE (just before the IndexOptimize part): change 'USE [master]' to 'USE [sqls_21]' and @CreateJobs to 'N'.
   The DBCC WRITEPAGE part damages the database on purpose: run it only on this throwaway database. */
USE sqls_21;
GO
SET NOCOUNT ON;
DELETE FROM dbo.OrderItems WHERE OrderId % 5 = 0 AND OrderId <= 600000;
INSERT dbo.OrderItems (OrderId, BookId, Quantity, UnitPrice)
SELECT 900000 + g.value / 2, 1 + (g.value * 7919) % 40000, 1, 10.00
FROM GENERATE_SERIES(1, 200000) AS g;
GO
SET NOCOUNT ON;
SELECT CAST(ips.avg_fragmentation_in_percent AS decimal(5,1)) AS frag_pct,
       CAST(ips.avg_page_space_used_in_percent AS decimal(5,1)) AS density_pct,
       ips.page_count, sp.rows, sp.rows_sampled
FROM sys.dm_db_index_physical_stats(DB_ID(), OBJECT_ID('dbo.OrderItems'), 4, NULL, 'DETAILED') AS ips
CROSS APPLY sys.dm_db_stats_properties(ips.object_id, ips.index_id) AS sp
WHERE ips.index_level = 0;
SET STATISTICS IO ON;
SELECT COUNT_BIG(*) AS lines FROM dbo.OrderItems WITH (INDEX (IX_OrderItems_BookId));
SET STATISTICS IO OFF;
SELECT CAST(log_since_last_log_backup_mb AS decimal(8,1)) AS log_mb FROM sys.dm_db_log_stats(DB_ID());
GO
ALTER DATABASE sqls_21 SET RECOVERY FULL;
GO
BACKUP DATABASE sqls_21 TO DISK = '/var/opt/mssql/backup/sqls_21_month.bak' WITH COPY_ONLY, COMPRESSION, INIT;
GO
USE master;
GO
RESTORE DATABASE sqls_21 FROM DISK = '/var/opt/mssql/backup/sqls_21_month.bak' WITH REPLACE;
GO
USE sqls_21;
GO
BACKUP DATABASE sqls_21 TO DISK = '/var/opt/mssql/backup/sqls_21_full.bak' WITH COMPRESSION, INIT;
BACKUP LOG sqls_21 TO DISK = '/var/opt/mssql/backup/sqls_21_log.trn' WITH INIT;
GO
ALTER INDEX IX_OrderItems_BookId ON dbo.OrderItems REORGANIZE;
GO
SET NOCOUNT ON;
SELECT CAST(ips.avg_fragmentation_in_percent AS decimal(5,1)) AS frag_pct,
       CAST(ips.avg_page_space_used_in_percent AS decimal(5,1)) AS density_pct,
       ips.page_count, sp.rows, sp.rows_sampled
FROM sys.dm_db_index_physical_stats(DB_ID(), OBJECT_ID('dbo.OrderItems'), 4, NULL, 'DETAILED') AS ips
CROSS APPLY sys.dm_db_stats_properties(ips.object_id, ips.index_id) AS sp
WHERE ips.index_level = 0;
SET STATISTICS IO ON;
SELECT COUNT_BIG(*) AS lines FROM dbo.OrderItems WITH (INDEX (IX_OrderItems_BookId));
SET STATISTICS IO OFF;
SELECT CAST(log_since_last_log_backup_mb AS decimal(8,1)) AS log_mb FROM sys.dm_db_log_stats(DB_ID());
GO
USE master;
GO
RESTORE DATABASE sqls_21 FROM DISK = '/var/opt/mssql/backup/sqls_21_month.bak' WITH REPLACE;
GO
USE sqls_21;
GO
BACKUP DATABASE sqls_21 TO DISK = '/var/opt/mssql/backup/sqls_21_full.bak' WITH COMPRESSION, INIT;
BACKUP LOG sqls_21 TO DISK = '/var/opt/mssql/backup/sqls_21_log.trn' WITH INIT;
GO
ALTER INDEX IX_OrderItems_BookId ON dbo.OrderItems REBUILD;
GO
SET NOCOUNT ON;
SELECT CAST(ips.avg_fragmentation_in_percent AS decimal(5,1)) AS frag_pct,
       CAST(ips.avg_page_space_used_in_percent AS decimal(5,1)) AS density_pct,
       ips.page_count, sp.rows, sp.rows_sampled
FROM sys.dm_db_index_physical_stats(DB_ID(), OBJECT_ID('dbo.OrderItems'), 4, NULL, 'DETAILED') AS ips
CROSS APPLY sys.dm_db_stats_properties(ips.object_id, ips.index_id) AS sp
WHERE ips.index_level = 0;
SET STATISTICS IO ON;
SELECT COUNT_BIG(*) AS lines FROM dbo.OrderItems WITH (INDEX (IX_OrderItems_BookId));
SET STATISTICS IO OFF;
SELECT CAST(log_since_last_log_backup_mb AS decimal(8,1)) AS log_mb FROM sys.dm_db_log_stats(DB_ID());
GO
USE master;
GO
RESTORE DATABASE sqls_21 FROM DISK = '/var/opt/mssql/backup/sqls_21_month.bak' WITH REPLACE;
GO
USE sqls_21;
GO
BACKUP DATABASE sqls_21 TO DISK = '/var/opt/mssql/backup/sqls_21_full.bak' WITH COMPRESSION, INIT;
BACKUP LOG sqls_21 TO DISK = '/var/opt/mssql/backup/sqls_21_log.trn' WITH INIT;
GO
ALTER INDEX IX_OrderItems_BookId ON dbo.OrderItems REBUILD WITH (ONLINE = ON);
GO
SET NOCOUNT ON;
SELECT CAST(ips.avg_fragmentation_in_percent AS decimal(5,1)) AS frag_pct,
       CAST(ips.avg_page_space_used_in_percent AS decimal(5,1)) AS density_pct,
       ips.page_count, sp.rows, sp.rows_sampled
FROM sys.dm_db_index_physical_stats(DB_ID(), OBJECT_ID('dbo.OrderItems'), 4, NULL, 'DETAILED') AS ips
CROSS APPLY sys.dm_db_stats_properties(ips.object_id, ips.index_id) AS sp
WHERE ips.index_level = 0;
SET STATISTICS IO ON;
SELECT COUNT_BIG(*) AS lines FROM dbo.OrderItems WITH (INDEX (IX_OrderItems_BookId));
SET STATISTICS IO OFF;
SELECT CAST(log_since_last_log_backup_mb AS decimal(8,1)) AS log_mb FROM sys.dm_db_log_stats(DB_ID());
GO
USE master;
GO
RESTORE DATABASE sqls_21 FROM DISK = '/var/opt/mssql/backup/sqls_21_month.bak' WITH REPLACE;
GO
USE sqls_21;
GO
BACKUP DATABASE sqls_21 TO DISK = '/var/opt/mssql/backup/sqls_21_full.bak' WITH COMPRESSION, INIT;
BACKUP LOG sqls_21 TO DISK = '/var/opt/mssql/backup/sqls_21_log.trn' WITH INIT;
GO
ALTER INDEX IX_OrderItems_BookId ON dbo.OrderItems REBUILD WITH (ONLINE = ON, RESUMABLE = ON);
GO
SET NOCOUNT ON;
SELECT CAST(ips.avg_fragmentation_in_percent AS decimal(5,1)) AS frag_pct,
       CAST(ips.avg_page_space_used_in_percent AS decimal(5,1)) AS density_pct,
       ips.page_count, sp.rows, sp.rows_sampled
FROM sys.dm_db_index_physical_stats(DB_ID(), OBJECT_ID('dbo.OrderItems'), 4, NULL, 'DETAILED') AS ips
CROSS APPLY sys.dm_db_stats_properties(ips.object_id, ips.index_id) AS sp
WHERE ips.index_level = 0;
SET STATISTICS IO ON;
SELECT COUNT_BIG(*) AS lines FROM dbo.OrderItems WITH (INDEX (IX_OrderItems_BookId));
SET STATISTICS IO OFF;
SELECT CAST(log_since_last_log_backup_mb AS decimal(8,1)) AS log_mb FROM sys.dm_db_log_stats(DB_ID());
GO
USE master;
GO
RESTORE DATABASE sqls_21 FROM DISK = '/var/opt/mssql/backup/sqls_21_month.bak' WITH REPLACE;
GO
USE sqls_21;
GO
BACKUP DATABASE sqls_21 TO DISK = '/var/opt/mssql/backup/sqls_21_full.bak' WITH COMPRESSION, INIT;
BACKUP LOG sqls_21 TO DISK = '/var/opt/mssql/backup/sqls_21_log.trn' WITH INIT;
GO
ALTER INDEX IX_OrderItems_BookId ON dbo.OrderItems REBUILD;
UPDATE STATISTICS dbo.OrderItems IX_OrderItems_BookId;
GO
SET NOCOUNT ON;
SELECT CAST(ips.avg_fragmentation_in_percent AS decimal(5,1)) AS frag_pct,
       CAST(ips.avg_page_space_used_in_percent AS decimal(5,1)) AS density_pct,
       ips.page_count, sp.rows, sp.rows_sampled
FROM sys.dm_db_index_physical_stats(DB_ID(), OBJECT_ID('dbo.OrderItems'), 4, NULL, 'DETAILED') AS ips
CROSS APPLY sys.dm_db_stats_properties(ips.object_id, ips.index_id) AS sp
WHERE ips.index_level = 0;
SET STATISTICS IO ON;
SELECT COUNT_BIG(*) AS lines FROM dbo.OrderItems WITH (INDEX (IX_OrderItems_BookId));
SET STATISTICS IO OFF;
SELECT CAST(log_since_last_log_backup_mb AS decimal(8,1)) AS log_mb FROM sys.dm_db_log_stats(DB_ID());
GO
/* Resumable rebuild: run the next statement in session 1, the PAUSE batch in session 2, then RESUME. */
ALTER INDEX PK_OrderItems ON dbo.OrderItems REBUILD WITH (ONLINE = ON, RESUMABLE = ON, MAXDOP = 1);
GO
ALTER INDEX PK_OrderItems ON dbo.OrderItems PAUSE;
SELECT name, state_desc, CAST(percent_complete AS decimal(5,1)) AS percent_complete, page_count
FROM sys.index_resumable_operations;
GO
ALTER INDEX PK_OrderItems ON dbo.OrderItems RESUME;
SELECT COUNT(*) AS resumable_ops FROM sys.index_resumable_operations;
GO
SELECT TOP (1) allocated_page_file_id, allocated_page_page_id FROM sys.dm_db_database_page_allocations(DB_ID(), OBJECT_ID('dbo.OrderItems'), 4, NULL, 'DETAILED') WHERE page_type_desc = 'INDEX_PAGE' AND page_level = 0 ORDER BY allocated_page_page_id DESC;
GO
/* Replace 58908 with the page id returned above. */
USE master;
GO
ALTER DATABASE sqls_21 SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DBCC WRITEPAGE (N'sqls_21', 1, 58908, 4000, 4, 0x00000000, 1);
ALTER DATABASE sqls_21 SET MULTI_USER;
GO
USE sqls_21;
GO
SELECT COUNT_BIG(*) AS lines FROM dbo.OrderItems WITH (INDEX (IX_OrderItems_BookId));
GO
DBCC CHECKDB (sqls_21) WITH NO_INFOMSGS, ALL_ERRORMSGS;
GO
DROP INDEX IX_OrderItems_BookId ON dbo.OrderItems;
CREATE INDEX IX_OrderItems_BookId ON dbo.OrderItems (BookId);
DBCC CHECKDB (sqls_21) WITH NO_INFOMSGS;
SELECT COUNT_BIG(*) AS lines FROM dbo.OrderItems WITH (INDEX (IX_OrderItems_BookId));
DELETE FROM msdb.dbo.suspect_pages WHERE database_id = DB_ID('sqls_21');
GO
USE master;
GO
RESTORE DATABASE sqls_21 FROM DISK = '/var/opt/mssql/backup/sqls_21_month.bak' WITH REPLACE;
GO
USE sqls_21;
GO
EXEC dbo.IndexOptimize
    @Databases = 'sqls_21',
    @FragmentationLow = NULL,
    @FragmentationMedium = 'INDEX_REORGANIZE,INDEX_REBUILD_ONLINE,INDEX_REBUILD_OFFLINE',
    @FragmentationHigh = 'INDEX_REBUILD_ONLINE,INDEX_REBUILD_OFFLINE',
    @FragmentationLevel1 = 5,
    @FragmentationLevel2 = 30,
    @MinNumberOfPages = 1000,
    @UpdateStatistics = 'ALL',
    @OnlyModifiedStatistics = 'Y',
    @LogToTable = 'Y',
    @Execute = 'N';
GO
EXEC dbo.IndexOptimize
    @Databases = 'sqls_21',
    @FragmentationLow = NULL,
    @FragmentationMedium = 'INDEX_REORGANIZE,INDEX_REBUILD_ONLINE,INDEX_REBUILD_OFFLINE',
    @FragmentationHigh = 'INDEX_REBUILD_ONLINE,INDEX_REBUILD_OFFLINE',
    @FragmentationLevel1 = 5,
    @FragmentationLevel2 = 30,
    @MinNumberOfPages = 1000,
    @UpdateStatistics = 'ALL',
    @OnlyModifiedStatistics = 'Y',
    @LogToTable = 'Y',
    @Execute = 'Y';
GO
SELECT CommandType, ISNULL(IndexName, StatisticsName) AS object_name,
       DATEDIFF(MILLISECOND, StartTime, EndTime) AS ms, ErrorNumber
FROM dbo.CommandLog
WHERE DatabaseName = 'sqls_21' AND ErrorNumber IS NOT NULL
ORDER BY ID;
GO
EXEC dbo.DatabaseIntegrityCheck @Databases = 'sqls_21', @CheckCommands = 'CHECKDB', @LogToTable = 'Y';
EXEC dbo.DatabaseBackup
    @Databases = 'sqls_21',
    @Directory = '/var/opt/mssql/backup',
    @DirectoryStructure = '{DatabaseName}{DirectorySeparator}{BackupType}',
    @FileName = '{DatabaseName}_{BackupType}_{Year}{Month}{Day}_{Hour}{Minute}{Second}.{FileExtension}',
    @BackupType = 'FULL',
    @Compress = 'Y',
    @CheckSum = 'Y',
    @Verify = 'Y',
    @LogToTable = 'Y';
GO
DBCC CHECKDB (sqls_21) WITH PHYSICAL_ONLY;
GO
