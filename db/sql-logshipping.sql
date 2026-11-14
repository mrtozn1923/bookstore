BACKUP DATABASE dbscaleseries_04 TO DISK = '/tmp/dbscaleseries_04_full.bak' WITH INIT, COMPRESSION;

RESTORE DATABASE dbscaleseries_04_standby FROM DISK = '/tmp/dbscaleseries_04_full.bak'
WITH MOVE 'dbscaleseries_04' TO '/var/opt/mssql/data/dbscaleseries_04_standby.mdf',
     MOVE 'dbscaleseries_04_log' TO '/var/opt/mssql/data/dbscaleseries_04_standby.ldf',
     STANDBY = '/tmp/dbscaleseries_04_undo.tuf';

BACKUP LOG dbscaleseries_04 TO DISK = '/tmp/dbscaleseries_04_log1.trn' WITH INIT;

RESTORE LOG dbscaleseries_04_standby FROM DISK = '/tmp/dbscaleseries_04_log1.trn'
WITH STANDBY = '/tmp/dbscaleseries_04_undo.tuf';
