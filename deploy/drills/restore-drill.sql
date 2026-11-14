/* Restore drill for BookstoreDb: take a backup, restore it under another name, check it, compare, measure, drop. */
/* Run monthly and after every change to the backup job: */
/*   sqlcmd -S <server> -U <user> -C -b -i deploy/drills/restore-drill.sql -v BackupDir="/var/opt/mssql/data" */
/* -b stops at the first error, so a failed step fails the scheduled job (and its alert). */
:setvar SourceDb BookstoreDb
:setvar DrillDb BookstoreDb_RestoreDrill
SET NOCOUNT ON;

/* 1. How old is the last full backup the backup job made? (RPO: the most data we would lose today.) */
SELECT TOP (1) 'last full backup' AS step, backup_finish_date AS finished_utc,
       DATEDIFF(MINUTE, backup_finish_date, SYSUTCDATETIME()) AS age_minutes
FROM msdb.dbo.backupset
WHERE database_name = '$(SourceDb)' AND type = 'D' AND name IS DISTINCT FROM 'restore-drill'
ORDER BY backup_finish_date DESC;

/* 2. A fresh backup with checksums (the drill does not rely on the job's file). */
DECLARE @file nvarchar(400) = N'$(BackupDir)/$(SourceDb)-drill.bak';
DECLARE @t0 datetime2 = SYSUTCDATETIME();
BACKUP DATABASE [$(SourceDb)] TO DISK = @file WITH INIT, CHECKSUM, COMPRESSION, NAME = 'restore-drill';
SELECT 'backup' AS step, DATEDIFF(MILLISECOND, @t0, SYSUTCDATETIME()) AS ms;

/* 3. VERIFYONLY reads the file and its checksums. It does NOT prove that a restore works; step 4 does. */
RESTORE VERIFYONLY FROM DISK = @file WITH CHECKSUM;

/* 4. Restore under another name, next to the original. */
DECLARE @data nvarchar(200), @log nvarchar(200);
SELECT @data = name FROM sys.master_files WHERE database_id = DB_ID('$(SourceDb)') AND type = 0;
SELECT @log = name FROM sys.master_files WHERE database_id = DB_ID('$(SourceDb)') AND type = 1;
DECLARE @dataFile nvarchar(400) = N'$(BackupDir)/$(DrillDb).mdf', @logFile nvarchar(400) = N'$(BackupDir)/$(DrillDb)_log.ldf';
SET @t0 = SYSUTCDATETIME();
RESTORE DATABASE [$(DrillDb)] FROM DISK = @file
    WITH MOVE @data TO @dataFile, MOVE @log TO @logFile, REPLACE, CHECKSUM;
SELECT 'restore' AS step, DATEDIFF(MILLISECOND, @t0, SYSUTCDATETIME()) AS ms;

/* 5. Is the restored database consistent? */
SET @t0 = SYSUTCDATETIME();
DBCC CHECKDB ([$(DrillDb)]) WITH NO_INFOMSGS;
SELECT 'checkdb' AS step, DATEDIFF(MILLISECOND, @t0, SYSUTCDATETIME()) AS ms;

/* 6. Does it hold what the application needs? Row counts against the source at backup time. */
SELECT t.name AS [table], src.n AS source_rows, drill.n AS restored_rows
FROM (VALUES ('ordering.Orders'), ('ordering.OrderItems'), ('ordering.Customers'), ('inventory.StockItems'), ('catalog.Books')) AS t(name)
CROSS APPLY (SELECT SUM(p.rows) AS n FROM [$(SourceDb)].sys.partitions p
             WHERE p.object_id = OBJECT_ID('[$(SourceDb)].' + t.name) AND p.index_id IN (0, 1)) AS src
CROSS APPLY (SELECT SUM(p.rows) AS n FROM [$(DrillDb)].sys.partitions p
             WHERE p.object_id = OBJECT_ID('[$(DrillDb)].' + t.name) AND p.index_id IN (0, 1)) AS drill;
SELECT 'newest order in the restore' AS step, MAX(PlacedAtUtc) AS placed_at_utc FROM [$(DrillDb)].ordering.Orders;

/* 7. Clean up. The numbers above go into docs/drills/log.md. */
DROP DATABASE [$(DrillDb)];

/* 8. The drill fails (and so does its scheduled job) if the backup job made no full backup in the last 26 hours: */
/*    a backup job that fails silently is found here, not on the day we need it. */
IF NOT EXISTS (SELECT 1 FROM msdb.dbo.backupset
               WHERE database_name = '$(SourceDb)' AND type = 'D' AND name IS DISTINCT FROM 'restore-drill'
                 AND backup_finish_date > DATEADD(HOUR, -26, GETDATE()))
    THROW 50001, 'No full backup of the source database from the backup job in the last 26 hours.', 1;
