sudo /opt/mssql/bin/mssql-conf set sqlagent.enabled true
sudo systemctl restart mssql-server

docker run -d --name sql1 -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=$env:MSSQL_SA_PASSWORD" -e "MSSQL_PID=Developer" -e "MSSQL_AGENT_ENABLED=true" -p 1433:1433 -v bookstore-sql-data:/var/opt/mssql mcr.microsoft.com/mssql/server:2025-latest
