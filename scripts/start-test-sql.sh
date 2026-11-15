#!/usr/bin/env bash
# Starts a throwaway SQL Server container on the build agent for the integration tests
# and hands the connection string to later steps as a secret pipeline variable.
set -euo pipefail

container_name="bookstore-test-sql"
image="mcr.microsoft.com/mssql/server:2025-latest"

# A new random password on every run; it never leaves this agent.
MSSQL_SA_PASSWORD="Bk-$(openssl rand -hex 16)-9z"
export MSSQL_SA_PASSWORD
echo "##vso[task.setsecret]${MSSQL_SA_PASSWORD}"

# -e NAME without a value copies it from this shell, so the password is not on the command line.
docker run -d --name "$container_name" \
  -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD \
  -p 1433:1433 "$image" > /dev/null

for attempt in $(seq 1 30); do
  if SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" docker exec -e SQLCMDPASSWORD "$container_name" \
      /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -Q "SELECT 1" > /dev/null 2>&1; then
    echo "SQL Server is ready (attempt $attempt)."
    connection="Server=localhost,1433;Database=BookstoreDb;User Id=sa;Password=${MSSQL_SA_PASSWORD};TrustServerCertificate=True"
    echo "##vso[task.setvariable variable=TestDbConnection;issecret=true]${connection}"
    exit 0
  fi
  sleep 2
done

echo "##vso[task.logissue type=error]SQL Server did not become ready in 60 seconds."
docker logs "$container_name" | tail -n 20
exit 1
