docker volume create bookstore-sql-data
docker run -d --name sql1 -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=$env:MSSQL_SA_PASSWORD" -e "MSSQL_PID=Developer" -p 1433:1433 -v bookstore-sql-data:/var/opt/mssql mcr.microsoft.com/mssql/server:2025-latest
docker exec sql1 mkdir -p /var/opt/mssql/backup
docker cp sql1:/var/opt/mssql/backup/sqls_20_full.bak D:\backup\
