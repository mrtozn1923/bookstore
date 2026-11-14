#!/usr/bin/env bash
# Docker series 03: container lifecycle, signals, exit codes and restart policies.
# Run from the repository root after: dotnet publish src/Bookstore.Tools -c Release -o out
set -uo pipefail

# Lifecycle: create -> start -> stop -> kill
docker create --name dockerseries-web -p 8080:80 nginx:1.30-alpine
docker ps -a --format "table {{.Names}}\t{{.Status}}"
docker start dockerseries-web
sleep 1
curl -s localhost:8080 | grep -o "<title>.*</title>"
docker inspect -f "StopSignal={{.Config.StopSignal}}" dockerseries-web
docker stop dockerseries-web
docker inspect -f "Status={{.State.Status}} ExitCode={{.State.ExitCode}}" dockerseries-web
docker start dockerseries-web
docker kill dockerseries-web
docker ps -a --filter name=dockerseries-web --format "table {{.Names}}\t{{.Status}}"
docker rm dockerseries-web

# Exit codes
docker run --name dockerseries-exit0 alpine:3 sh -c "exit 0"
docker run --name dockerseries-exit1 alpine:3 sh -c "echo connection failed >&2; exit 1"
docker run --name dockerseries-oom --memory 64m alpine:3 sh -c "tail /dev/zero"
docker run --name dockerseries-segv alpine:3 sh -c 'kill -SEGV $$'
docker run --init --name dockerseries-segv-init alpine:3 sh -c 'kill -SEGV $$'
docker ps -a --filter name=dockerseries- --format "table {{.Names}}\t{{.Status}}"
docker inspect -f "{{.Name}} ExitCode={{.State.ExitCode}} OOMKilled={{.State.OOMKilled}}" dockerseries-exit1 dockerseries-oom
docker rm dockerseries-exit0 dockerseries-exit1 dockerseries-oom dockerseries-segv dockerseries-segv-init

# Bookstore.Tools with the Generic Host: graceful stop (exec form, dotnet is PID 1)
docker run -d --name dockerseries-tools -v "${PWD}/out:/app" -w /app mcr.microsoft.com/dotnet/runtime:10.0 dotnet Bookstore.Tools.dll
sleep 4
time docker stop dockerseries-tools
docker logs dockerseries-tools
docker inspect -f "ExitCode={{.State.ExitCode}}" dockerseries-tools
docker rm dockerseries-tools

# The same app behind a shell: SIGTERM never reaches dotnet, stop waits 10 s, exit 137
docker run -d --name dockerseries-tools -v "${PWD}/out:/app" -w /app mcr.microsoft.com/dotnet/runtime:10.0 sh -c 'dotnet Bookstore.Tools.dll; echo import done'
sleep 2
docker top dockerseries-tools -o pid,args
time docker stop dockerseries-tools
docker inspect -f "ExitCode={{.State.ExitCode}}" dockerseries-tools
docker rm dockerseries-tools

# Restart policy on-failure
docker run -d --name dockerseries-retry --restart on-failure:3 alpine:3 sh -c 'echo "$(date +%T) started"; exit 1'
sleep 6
docker logs dockerseries-retry
docker inspect -f "RestartCount={{.RestartCount}} Status={{.State.Status}} ExitCode={{.State.ExitCode}}" dockerseries-retry
docker rm dockerseries-retry
