#!/usr/bin/env bash
# 12 - three mongod processes in one container: a 3-member replica set with a small footprint (lab only)
set -euo pipefail
base=/data/db/rs3
mkdir -p "$base"
if [ ! -f "$base/keyfile" ]; then
    openssl rand -base64 756 > "$base/keyfile"
    chmod 400 "$base/keyfile"
fi
for port in 27021 27022 27023; do
    mkdir -p "$base/$port"
    mongod --replSet rs3 --port "$port" --bind_ip_all --dbpath "$base/$port" \
        --keyFile "$base/keyfile" --wiredTigerCacheSizeGB 0.25 \
        --logpath "$base/$port.log" --pidfilepath "$base/$port.pid" --fork
done
stop() {
    for port in 27021 27022 27023; do
        mongod --shutdown --dbpath "$base/$port" || true
    done
    exit 0
}
trap stop TERM INT
sleep infinity &
wait $!
