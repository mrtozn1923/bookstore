#!/usr/bin/env bash
# Docker series 11: BookstoreDb on SQL Server 2025 with volumes, bind mounts and tmpfs.
# Run from the repository root on a Linux engine (or Docker Desktop) with MSSQL_SA_PASSWORD exported.
set -uo pipefail
: "${MSSQL_SA_PASSWORD:?export MSSQL_SA_PASSWORD first}"

IMAGE=mcr.microsoft.com/mssql/server:2025-latest

sql() {   # sqlcmd inside the container; the password comes from the environment, not the command line
    docker exec -e SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" dockerseries-db \
        /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa "$@"
}
wait_for_sql() {
    until sql -Q "SELECT 1" > /dev/null 2>&1; do sleep 2; done
}

echo "### 1. No volume: the database lives in the container's writable layer"
docker image inspect $IMAGE --format 'User={{.Config.User}} Volumes={{json .Config.Volumes}}'
docker run -d --name dockerseries-db -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD \
    -v "$PWD/db:/scripts:ro" $IMAGE
wait_for_sql
sql -i /scripts/01-bookstore.sql
docker inspect dockerseries-db --format '{{range .Mounts}}{{.Type}} {{.Source}} -> {{.Destination}}{{println}}{{end}}'
docker rm -f dockerseries-db
docker run -d --name dockerseries-db -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD \
    -v "$PWD/db:/scripts:ro" $IMAGE
wait_for_sql
sql -h -1 -Q "SET NOCOUNT ON; SELECT name FROM sys.databases WHERE database_id > 4"
echo "(no user databases)"
docker rm -f dockerseries-db

echo "### 2. Named volume"
docker volume create dockerseries-sqldata
docker run -d --name dockerseries-db -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD \
    -p 127.0.0.1:1433:1433 \
    -v dockerseries-sqldata:/var/opt/mssql \
    -v "$PWD/db:/scripts:ro" \
    $IMAGE
wait_for_sql
sql -i /scripts/01-bookstore.sql
sql -d BookstoreDb -Q "INSERT INTO dbo.Books VALUES ('9780547928227', N'The Hobbit', N'J. R. R. Tolkien', 260.00)"
docker rm -f dockerseries-db
docker run -d --name dockerseries-db -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD \
    -p 127.0.0.1:1433:1433 \
    -v dockerseries-sqldata:/var/opt/mssql \
    -v "$PWD/db:/scripts:ro" \
    $IMAGE
wait_for_sql
sql -d BookstoreDb -Q "SET NOCOUNT ON; SELECT COUNT(*) AS Books FROM dbo.Books"
docker volume ls --filter name=dockerseries
docker volume inspect dockerseries-sqldata
docker inspect dockerseries-db --format '{{range .Mounts}}{{.Type}} {{.Name}}{{.Source}} -> {{.Destination}} rw={{.RW}}{{println}}{{end}}'
docker system df -v | grep -A3 "^VOLUME NAME"

echo "### 3. Backups"
# a) SQL Server's own backup, while the database is online
sql -Q "BACKUP DATABASE BookstoreDb TO DISK = N'/var/opt/mssql/backup/BookstoreDb.bak' WITH INIT, COMPRESSION"
mkdir -p backup
docker cp dockerseries-db:/var/opt/mssql/backup/BookstoreDb.bak backup/
# b) The whole volume as a tar file, with the container stopped
docker stop dockerseries-db
docker run --rm -v dockerseries-sqldata:/data:ro -v "$PWD/backup:/backup" alpine:3 \
    tar czf /backup/sqldata.tar.gz -C /data .
ls -lh backup
docker volume create dockerseries-sqldata-restored
docker run --rm -v dockerseries-sqldata-restored:/data -v "$PWD/backup:/backup:ro" alpine:3 \
    sh -c "tar xzf /backup/sqldata.tar.gz -C /data && ls -ln /data"
docker rm dockerseries-db
docker run -d --name dockerseries-db -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD \
    -v dockerseries-sqldata-restored:/var/opt/mssql $IMAGE
wait_for_sql
sql -d BookstoreDb -Q "SET NOCOUNT ON; SELECT COUNT(*) AS Books FROM dbo.Books"
docker rm -f dockerseries-db

echo "### 4. Bind mount and the non-root user"
sudo_() { if [ "$(id -u)" = 0 ]; then "$@"; else sudo "$@"; fi; }
sudo_ mkdir -p /srv/bookstore/mssql
ls -ldn /srv/bookstore/mssql
docker run -d --name dockerseries-db -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD \
    -v /srv/bookstore/mssql:/var/opt/mssql $IMAGE
sleep 15
docker ps -a --filter name=dockerseries-db --format "{{.Names}}: {{.Status}}"
docker logs dockerseries-db 2>&1 | tail -4
docker rm -f dockerseries-db
sudo_ chown -R 10001:0 /srv/bookstore/mssql
docker run -d --name dockerseries-db -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD \
    -v /srv/bookstore/mssql:/var/opt/mssql $IMAGE
wait_for_sql
sudo_ ls -ln /srv/bookstore/mssql
docker rm -f dockerseries-db
sudo_ rm -rf /srv/bookstore

echo "### 5. tmpfs: a throwaway database for tests"
docker run -d --name dockerseries-db -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD \
    --mount type=tmpfs,destination=/var/opt/mssql,tmpfs-size=2g,tmpfs-mode=1777 \
    -v "$PWD/db:/scripts:ro" $IMAGE
wait_for_sql
sql -i /scripts/01-bookstore.sql
docker exec dockerseries-db df -h /var/opt/mssql
docker restart dockerseries-db
wait_for_sql
sql -h -1 -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name = N'BookstoreDb'"
docker rm -f dockerseries-db

echo "### 6. Anonymous volumes"
docker run --name dockerseries-anon -v /data alpine:3 sh -c "echo order-42 > /data/note.txt"
docker inspect dockerseries-anon --format '{{range .Mounts}}{{.Type}} {{.Name}} -> {{.Destination}}{{end}}'
docker rm dockerseries-anon
docker volume ls --filter dangling=true
docker run --rm -v /data alpine:3 sh -c "echo order-43 > /data/note.txt"
docker volume ls --filter dangling=true

echo "### 7. An empty volume is filled from the image, a non-empty one hides it"
docker run -d --name dockerseries-web -e API_BASE_URL=http://localhost:8080 \
    -v dockerseries-webroot:/usr/share/nginx/html dockerseries-bookstore-web:1.0.0
sleep 3
docker run --rm -v dockerseries-webroot:/data alpine:3 ls /data
docker rm -f dockerseries-web
# Another image (plain nginx) with the same volume still serves bookstore-web's files
docker run -d --name dockerseries-web -p 8081:8080 \
    -v dockerseries-webroot:/usr/share/nginx/html nginxinc/nginx-unprivileged:1.30-alpine
sleep 3
curl -s localhost:8081/ | grep -o "<title>.*</title>"
docker rm -f dockerseries-web

docker volume rm dockerseries-sqldata dockerseries-sqldata-restored dockerseries-webroot
