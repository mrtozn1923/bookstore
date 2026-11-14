#!/usr/bin/env bash
# Docker series 01: a container is an isolated process on a shared Linux kernel.
# Run from Git Bash/WSL or any Linux shell with Docker available. Cleans up after itself.
set -euo pipefail

docker run -d --name dockerseries-web --cpuset-cpus 0-1 -p 8081:80 nginx:1.30-alpine
sleep 1

# Inside the container: nginx is PID 1
docker exec dockerseries-web ps -o pid,ppid,user,args
docker exec dockerseries-web hostname

# Outside the container: the same processes with host PIDs
docker top dockerseries-web -o pid,ppid,uid,user,args
docker inspect -f '{{.State.Pid}}' dockerseries-web

# The process tree in the Linux VM/host: dockerd, containerd, shim, nginx
docker run --rm --pid=host alpine:3 ps -o pid,ppid,args

# cgroup limits are just files the kernel enforces
docker run --rm --memory 64m --cpus 0.5 alpine:3 cat /sys/fs/cgroup/memory.max /sys/fs/cgroup/cpu.max

# Different user space, same kernel
docker run --rm alpine:3 grep PRETTY_NAME /etc/os-release
docker run --rm mcr.microsoft.com/dotnet/aspnet:10.0 grep PRETTY_NAME /etc/os-release
[ "$(docker run --rm alpine:3 uname -r)" = "$(docker run --rm mcr.microsoft.com/dotnet/aspnet:10.0 uname -r)" ] && echo "same kernel"

# Engine details
docker info --format 'Server Version: {{.ServerVersion}}{{"\n"}}Storage Driver: {{.Driver}}{{"\n"}}Cgroup Version: {{.CgroupVersion}}{{"\n"}}Default Runtime: {{.DefaultRuntime}}{{"\n"}}OSType: {{.OSType}}'

# The same OCI image under a different engine and runtime (Podman + crun), Podman itself in a lab container
docker run --rm --name dockerseries-podman --privileged quay.io/podman/stable \
    sh -c 'podman --version; podman --storage-driver=vfs run --rm -q docker.io/library/nginx:1.30-alpine nginx -v; podman --storage-driver=vfs info --format "{{.Host.OCIRuntime.Name}}"'

docker rm -f dockerseries-web
