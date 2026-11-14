#!/usr/bin/env bash
# Docker series 13: hardening Bookstore's containers. Run from the repository root on a Linux engine
# with MSSQL_SA_PASSWORD and BOOKSTORE_DB_PASSWORD exported.
set -uo pipefail
: "${MSSQL_SA_PASSWORD:?}" "${BOOKSTORE_DB_PASSWORD:?}"
TAG=1.2.0
DB_CONN="Server=bookstore-db;Database=BookstoreDb;User Id=bookstore_app;Password=$BOOKSTORE_DB_PASSWORD;TrustServerCertificate=True"
sudo_() { if [ "$(id -u)" = 0 ]; then "$@"; else sudo "$@"; fi; }

docker build -q -f src/Bookstore.Api/Dockerfile -t dockerseries-bookstore-api:$TAG .
docker build -q -t dockerseries-bookstore-web:$TAG web
docker network create dockerseries-net
docker run -d --name dockerseries-db --network dockerseries-net --network-alias bookstore-db \
    -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD -v dockerseries-sqldata:/var/opt/mssql \
    -v "$PWD/db:/scripts:ro" mcr.microsoft.com/mssql/server:2025-latest
until docker exec -e SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" dockerseries-db \
    /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -Q "SELECT 1" > /dev/null 2>&1; do sleep 2; done
docker exec -e SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" -e BOOKSTORE_DB_PASSWORD dockerseries-db \
    /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -i /scripts/01-bookstore.sql -i /scripts/02-app-login.sql > /dev/null

echo "### 1. A secret in an environment variable is part of the container's config"
docker run -d --name dockerseries-api --network dockerseries-net --network-alias bookstore-api \
    -e ConnectionStrings__Default="$DB_CONN" dockerseries-bookstore-api:$TAG
docker inspect dockerseries-api --format '{{range .Config.Env}}{{println .}}{{end}}' | grep ConnectionStrings \
    | sed "s/$BOOKSTORE_DB_PASSWORD/<password>/"
docker rm -f dockerseries-api

echo "### 2. The same secret as a read-only file"
mkdir -p secrets
printf '%s' "$DB_CONN" > secrets/ConnectionStrings__Default
sudo_ chown 1654 secrets/ConnectionStrings__Default
chmod 0400 secrets/ConnectionStrings__Default
ls -ln secrets

echo "### 3. Hardened API and web"
docker run -d --name dockerseries-api --network dockerseries-net --network-alias bookstore-api \
    --read-only --tmpfs /tmp:rw,size=16m \
    --cap-drop ALL --security-opt no-new-privileges \
    --memory 256m --cpus 1 --pids-limit 100 \
    -v "$PWD/secrets:/run/secrets:ro" \
    dockerseries-bookstore-api:$TAG
docker run -d --name dockerseries-web --network dockerseries-net -p 127.0.0.1:8081:8080 \
    --read-only --tmpfs /tmp:rw,size=8m \
    --cap-drop ALL --security-opt no-new-privileges \
    --memory 64m --pids-limit 50 \
    -e API_BASE_URL=/api dockerseries-bookstore-web:$TAG
sleep 20
docker inspect dockerseries-api --format '{{range .Config.Env}}{{println .}}{{end}}' | grep -c ConnectionStrings
docker inspect dockerseries-api --format \
    'User={{.Config.User}} ReadonlyRootfs={{.HostConfig.ReadonlyRootfs}} CapDrop={{.HostConfig.CapDrop}} SecurityOpt={{.HostConfig.SecurityOpt}} PidsLimit={{.HostConfig.PidsLimit}}'
docker ps --format "table {{.Names}}\t{{.Status}}"
curl -s localhost:8081/api/books | jq -r '.[0:2][] | .title'
docker logs dockerseries-web 2>&1 | grep -E "40-runtime|error" | head -3

echo "### 4. Capabilities"
docker run --rm alpine:3 sh -c 'grep CapEff /proc/self/status; chown 1000 /etc/hostname && echo "chown: ok"'
docker run --rm --cap-drop ALL alpine:3 sh -c 'grep CapEff /proc/self/status; chown 1000 /etc/hostname || echo "chown: failed"'

echo "### 5. --pids-limit against a fork bomb"
docker run --rm --pids-limit 50 alpine:3 sh -c 'for i in $(seq 1 100); do sleep 30 & done; wait' 2>&1 | sort | uniq -c | head -3

echo "### 6. The Docker socket is root on the host"
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock docker:29.9.3-cli \
    docker run --rm -v /:/host alpine:3 sh -c 'id; head -1 /host/etc/shadow'

echo "### 7. Scanning with Trivy"
mkdir -p scan
docker save -o scan/api.tar dockerseries-bookstore-api:$TAG
docker save --platform linux/amd64 -o scan/aspnet.tar mcr.microsoft.com/dotnet/aspnet:10.0
TRIVY="docker run --rm -v $PWD/scan:/scan:ro -v dockerseries-trivy-cache:/root/.cache aquasec/trivy:0.76.2"
$TRIVY image --input /scan/api.tar --scanners vuln
$TRIVY image --quiet --input /scan/aspnet.tar --scanners vuln | grep -E "^Total"
$TRIVY image --quiet --input /scan/api.tar --scanners vuln --severity HIGH,CRITICAL --ignore-unfixed --exit-code 1
echo "exit code: $?"

docker rm -f dockerseries-web dockerseries-api dockerseries-db
docker network rm dockerseries-net
