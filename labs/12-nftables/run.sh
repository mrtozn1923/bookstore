#!/usr/bin/env bash
# Docker series 12: Engine 29's experimental nftables firewall backend in a throwaway Docker-in-Docker engine.
set -uo pipefail
docker run -d --privileged --name dockerseries-dind-nft --entrypoint sh docker:29.9.3-dind \
    -c 'apk add -q nftables && exec dockerd-entrypoint.sh --firewall-backend=nftables'
sleep 15
docker exec dockerseries-dind-nft sh -c '
    docker info | grep "Firewall Backend"
    docker run -d --name dockerseries-web -p 127.0.0.1:8081:8080 nginxinc/nginx-unprivileged:1.30-alpine > /dev/null
    sleep 2
    nft list tables
    nft list ruleset | grep "dport 8081"
    iptables -S DOCKER-USER'
docker rm -f -v dockerseries-dind-nft
