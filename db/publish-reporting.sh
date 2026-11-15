#!/bin/bash
# Build the SQL Database Project and publish it with SqlPackage 170.5.96. Needs MSSQL_SA_PASSWORD.
set -euo pipefail
cd Bookstore.Reporting.Db
dotnet build
sqlpackage /Action:Script /SourceFile:bin/Debug/Bookstore.Reporting.Db.dacpac /TargetServerName:"localhost,14333" /TargetDatabaseName:sqls_24_reporting /TargetUser:sa /TargetPassword:"$MSSQL_SA_PASSWORD" /TargetTrustServerCertificate:True /OutputPath:deploy.sql
sqlpackage /Action:Publish /SourceFile:bin/Debug/Bookstore.Reporting.Db.dacpac /TargetServerName:"localhost,14333" /TargetDatabaseName:sqls_24_reporting /TargetUser:sa /TargetPassword:"$MSSQL_SA_PASSWORD" /TargetTrustServerCertificate:True
