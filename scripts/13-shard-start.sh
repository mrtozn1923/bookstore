#!/usr/bin/env bash
# 13 - config server, two shards (each a one-member replica set) and a mongos in one container (lab only)
set -euo pipefail
base=/data/db/shard
mkdir -p "$base"
if [ ! -f "$base/keyfile" ]; then
    openssl rand -base64 756 > "$base/keyfile"
    chmod 400 "$base/keyfile"
fi
start() {   # name, port, role option
    mkdir -p "$base/$1"
    mongod "$3" --replSet "$1" --port "$2" --bind_ip_all --dbpath "$base/$1" --keyFile "$base/keyfile" \
        --wiredTigerCacheSizeGB 0.25 --logpath "$base/$1.log" --pidfilepath "$base/$1.pid" --fork
}
start cfg 27031 --configsvr
start shardA 27032 --shardsvr
start shardB 27033 --shardsvr
for rs in "cfg 27031 true" "shardA 27032 false" "shardB 27033 false"; do
    set -- $rs
    mongosh --quiet --port "$2" --eval "try { rs.initiate({ _id: '$1', configsvr: $3, members: [{ _id: 0, host: 'localhost:$2' }] }) } catch (e) {}" > /dev/null
done
until mongosh --quiet --port 27031 --eval 'db.hello().isWritablePrimary' 2>/dev/null | grep -q true; do sleep 1; done
mongos --configdb cfg/localhost:27031 --port 27030 --bind_ip_all --keyFile "$base/keyfile" \
    --logpath "$base/mongos.log" --pidfilepath "$base/mongos.pid" --fork
stop() {
    mongosh --quiet --port 27030 admin --eval 'db.shutdownServer()' > /dev/null 2>&1 || true
    for name in shardB shardA cfg; do mongod --shutdown --dbpath "$base/$name" || true; done
    exit 0
}
trap stop TERM INT
sleep infinity &
wait $!
