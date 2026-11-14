#!/usr/bin/env bash
# Docker series 14: a systematic way to debug containers. Run from the repository root.
set -uo pipefail
TAG=1.2.0
docker build -q -f src/Bookstore.Api/Dockerfile -t dockerseries-bookstore-api:$TAG .
docker build -q -t dockerseries-bookstore-web:$TAG web
docker network create dockerseries-net

echo "### 1. It does not start"
docker run --rm --entrypoint dotnet dockerseries-bookstore-api:$TAG Bookstore.Ap.dll; echo "exit code: $?"
docker build -q -t dockerseries-job:noshebang labs/14-exec-format
docker build -q -t dockerseries-job:crlf --build-arg SCRIPT=job-crlf.sh labs/14-exec-format
docker run --name dockerseries-job dockerseries-job:noshebang; echo "exit code: $?"
docker rm dockerseries-job
docker run --name dockerseries-job dockerseries-job:crlf; echo "exit code: $?"
docker rm dockerseries-job
docker image inspect dockerseries-bookstore-api:$TAG --format '{{.Os}}/{{.Architecture}}'

echo "### 2. It starts and dies"
docker run -d --name dockerseries-web --restart on-failure:3 dockerseries-bookstore-web:$TAG
sleep 8
docker ps -a --filter name=dockerseries-web --format "{{.Names}}  {{.Status}}"
docker inspect dockerseries-web --format 'ExitCode={{.State.ExitCode}} RestartCount={{.RestartCount}} OOMKilled={{.State.OOMKilled}}'
docker logs --tail 1 dockerseries-web
docker rm -f dockerseries-web

echo "### 3. Shell-less container: a helper container in its namespaces"
docker run -d --name dockerseries-api --network dockerseries-net \
    --read-only --tmpfs /tmp --cap-drop ALL --security-opt no-new-privileges \
    --cpus 1 --memory 256m dockerseries-bookstore-api:$TAG
sleep 6
docker exec dockerseries-api sh; echo "exit code: $?"
docker run --rm --pid container:dockerseries-api --network container:dockerseries-api busybox:1.37 \
    sh -c 'ps; wget -qO- localhost:8080/health; echo; netstat -tln; ls /proc/1/root/app'
docker run --rm --pid container:dockerseries-api --network container:dockerseries-api --cap-add SYS_PTRACE busybox:1.37 \
    sh -c 'ls /proc/1/root/app | head -3; ls /proc/1/root/tmp; tr "\0" "\n" < /proc/1/environ | grep ASPNETCORE'
docker rm -f dockerseries-api

echo "### 4. Exit code 137: the API dies about 30 seconds after it starts"
docker events --filter container=dockerseries-api --format '{{.Time}} {{.Action}}' > /tmp/dockerseries-events.txt &
EVENTS_PID=$!
docker run -d --name dockerseries-api --network dockerseries-net --cpus 2 --memory 32m dockerseries-bookstore-api:$TAG
sleep 12
docker stats --no-stream --format "{{.Name}}  {{.MemUsage}}  {{.MemPerc}}  {{.PIDs}}" dockerseries-api
docker logs dockerseries-api 2>&1 | grep -A1 "GC memory" | tail -1
sleep 40
docker ps -a --filter name=dockerseries-api --format "{{.Names}}  {{.Status}}"
docker inspect dockerseries-api --format '{{json .State}}' \
    | jq '{Status, ExitCode, OOMKilled, StartedAt, FinishedAt, Health: .Health.Status, LastProbe: .Health.Log[-1]}'
kill $EVENTS_PID
while read -r t action; do echo "$(date -u -d "@$t" +%H:%M:%S) $action"; done < /tmp/dockerseries-events.txt
docker rm -f dockerseries-api

echo "### 5. The fix: a limit with headroom, measured under load"
docker run -d --name dockerseries-api --network dockerseries-net --cpus 2 --memory 128m dockerseries-bookstore-api:$TAG
sleep 8
docker run -d --name dockerseries-load --network dockerseries-net alpine:3 sh -c \
    'for i in $(seq 1 20); do (for j in $(seq 1 300); do wget -qO- http://dockerseries-api:8080/books > /dev/null; done) & done; wait'
for i in 1 2 3 4 5 6; do
    docker stats --no-stream --format "{{.Name}}  {{.MemUsage}}  {{.MemPerc}}  {{.PIDs}}" dockerseries-api
    sleep 5
done
sleep 30
docker ps -a --filter name=dockerseries-api --format "{{.Names}}  {{.Status}}"
docker inspect dockerseries-api --format 'OOMKilled={{.State.OOMKilled}} RestartCount={{.RestartCount}}'
docker rm -f dockerseries-load dockerseries-api

echo "### 6. Disk usage"
docker system df

docker network rm dockerseries-net
docker rmi dockerseries-job:noshebang dockerseries-job:crlf
