#!/bin/bash
# SqlPackage 170.5.96. Needs MSSQL_SA_PASSWORD.
set -euo pipefail
sqlpackage /Action:Export /SourceServerName:"localhost,14333" /SourceDatabaseName:sqls_23_anon /SourceUser:sa /SourcePassword:"$MSSQL_SA_PASSWORD" /SourceTrustServerCertificate:True /TargetFile:bookstore-dev.bacpac
sqlpackage /Action:Import /TargetServerName:"localhost,14333" /TargetDatabaseName:sqls_23_dev /TargetUser:sa /TargetPassword:"$MSSQL_SA_PASSWORD" /TargetTrustServerCertificate:True /SourceFile:bookstore-dev.bacpac
