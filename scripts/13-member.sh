#!/usr/bin/env bash
# 13 - stop, kill or start one member of the three-member lab: 13-member.sh stop|kill|start <port>
set -euo pipefail
action=$1
port=$2
base=/data/db/rs3
case "$action" in
    stop) mongod --shutdown --dbpath "$base/$port" ;;
    kill) kill -9 "$(cat "$base/$port.pid")" ;;
    start) mongod --replSet rs3 --port "$port" --bind_ip_all --dbpath "$base/$port" \
               --keyFile "$base/keyfile" --wiredTigerCacheSizeGB 0.25 \
               --logpath "$base/$port.log" --logappend --pidfilepath "$base/$port.pid" --fork ;;
esac
