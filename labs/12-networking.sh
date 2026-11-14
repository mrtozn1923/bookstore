#!/usr/bin/env bash
# Docker series 12: Bookstore.Api, BookstoreDb and bookstore-web on a user-defined network.
# Run from the repository root with MSSQL_SA_PASSWORD and BOOKSTORE_DB_PASSWORD exported.
set -uo pipefail
: "${MSSQL_SA_PASSWORD:?}" "${BOOKSTORE_DB_PASSWORD:?}"
TAG=1.1.0
DB_CONN="Server=bookstore-db;Database=BookstoreDb;User Id=bookstore_app;Password=$BOOKSTORE_DB_PASSWORD;TrustServerCertificate=True"

sql() {
    docker exec -e SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" -e BOOKSTORE_DB_PASSWORD dockerseries-db \
        /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa "$@"
}
wait_for_sql() { until sql -Q "SELECT 1" > /dev/null 2>&1; do sleep 2; done; }

docker build -q -f src/Bookstore.Api/Dockerfile -t dockerseries-bookstore-api:$TAG .
docker build -q -t dockerseries-bookstore-web:$TAG web

echo "### 1. Networks every engine starts with"
docker network ls

echo "### 2. Database and API on the default bridge"
docker run -d --name dockerseries-db -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD \
    -v dockerseries-sqldata:/var/opt/mssql -v "$PWD/db:/scripts:ro" \
    mcr.microsoft.com/mssql/server:2025-latest
wait_for_sql
sql -i /scripts/01-bookstore.sql -i /scripts/02-app-login.sql
docker run -d --name dockerseries-api -p 8080:8080 \
    -e ConnectionStrings__Default="${DB_CONN/bookstore-db/dockerseries-db}" \
    dockerseries-bookstore-api:$TAG
sleep 8
curl -s -o /dev/null -w "GET /books: %{http_code}\n" localhost:8080/books
docker logs dockerseries-api 2>&1 | grep -E "Catalog source|SqlException|provider" | head -4
docker network inspect bridge --format '{{range .Containers}}{{.Name}} {{.IPv4Address}}{{println}}{{end}}'
docker rm -f dockerseries-api

echo "### 3. A user-defined network"
docker network create dockerseries-net
docker network connect --alias bookstore-db dockerseries-net dockerseries-db
docker network disconnect bridge dockerseries-db
docker run -d --name dockerseries-api --network dockerseries-net --network-alias bookstore-api \
    -e ConnectionStrings__Default="$DB_CONN" \
    dockerseries-bookstore-api:$TAG
sleep 8
docker logs dockerseries-api 2>&1 | grep -A1 "Catalog source"
docker run --rm --network dockerseries-net alpine:3 sh -c "cat /etc/resolv.conf | grep nameserver; nslookup bookstore-db | tail -2; wget -qO- http://bookstore-api:8080/books | head -c 120; echo"
docker network inspect dockerseries-net --format '{{range .Containers}}{{.Name}} {{.IPv4Address}}{{println}}{{end}}'

echo "### 4. Only the entry point is published"
docker run -d --name dockerseries-web --network dockerseries-net -p 127.0.0.1:8081:8080 \
    -e API_BASE_URL=/api -e APP_ENVIRONMENT=staging dockerseries-bookstore-web:$TAG
sleep 3
curl -s localhost:8081/config.json
curl -s localhost:8081/api/books | jq -r '.[0:3][] | "\(.title) | \(.priceTry)"'
curl -s -o /dev/null -w "GET :8080/books from the host: %{http_code}\n" -m 3 localhost:8080/books
docker ps --format "table {{.Names}}\t{{.Ports}}"

echo "### 5. 0.0.0.0 versus 127.0.0.1"
SERVER_IP=$(hostname -i | awk '{print $1}')
docker rm -f dockerseries-api
docker run -d --name dockerseries-api --network dockerseries-net --network-alias bookstore-api \
    -p 8080:8080 -e ConnectionStrings__Default="$DB_CONN" dockerseries-bookstore-api:$TAG
sleep 6
docker port dockerseries-api
curl -s -o /dev/null -w "8080 -> %{http_code}\n" -m 3 "http://$SERVER_IP:8080/health"
curl -sS -o /dev/null -m 3 "http://$SERVER_IP:8081/"
iptables -t nat -S DOCKER | grep -E "dport (8080|8081)"
iptables -S DOCKER-USER

echo "### 6. The app listens on 127.0.0.1 inside the container"
docker run -d --name dockerseries-api2 -p 127.0.0.1:8082:8080 \
    -e ASPNETCORE_URLS=http://127.0.0.1:8080 dockerseries-bookstore-api:$TAG
sleep 6
docker logs dockerseries-api2 2>&1 | grep "Now listening"
curl -sS -m 3 localhost:8082/health; echo " (exit $?)"
docker rm -f dockerseries-api2

echo "### 7. From a container to the host: host.docker.internal"
docker run --rm --add-host host.docker.internal:host-gateway alpine:3 grep host.docker.internal /etc/hosts
HOST_CONN="${DB_CONN/bookstore-db/host.docker.internal,1433}"
for publish in 127.0.0.1:1433:1433 1433:1433; do
    docker rm -f dockerseries-db dockerseries-api2 > /dev/null 2>&1
    docker run -d --name dockerseries-db -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD -p $publish \
        -v dockerseries-sqldata:/var/opt/mssql mcr.microsoft.com/mssql/server:2025-latest
    wait_for_sql
    docker run -d --name dockerseries-api2 -p 127.0.0.1:8082:8080 \
        --add-host host.docker.internal:host-gateway \
        -e ConnectionStrings__Default="$HOST_CONN" dockerseries-bookstore-api:$TAG
    sleep 8
    curl -s -o /dev/null -w "db published as $publish -> GET /books: %{http_code}\n" -m 60 localhost:8082/books
done
docker rm -f dockerseries-api2 dockerseries-api dockerseries-web dockerseries-db

echo "### 8. Without TrustServerCertificate (self-signed certificate)"
docker run -d --name dockerseries-db --network dockerseries-net --network-alias bookstore-db \
    -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD -v dockerseries-sqldata:/var/opt/mssql \
    mcr.microsoft.com/mssql/server:2025-latest
wait_for_sql
docker run -d --name dockerseries-api --network dockerseries-net \
    -e ConnectionStrings__Default="${DB_CONN%;TrustServerCertificate=True}" dockerseries-bookstore-api:$TAG
sleep 8
docker run --rm --network dockerseries-net alpine:3 wget -qO- -T 30 http://dockerseries-api:8080/books
docker logs dockerseries-api 2>&1 | grep -E "SqlException|--->" | head -2
docker rm -f dockerseries-api dockerseries-db
docker network rm dockerseries-net
