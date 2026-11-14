#!/bin/sh
# Post 13: a 3 primary + 3 replica cluster in a single container (lab only, never like this in production)
set -e
for port in 7000 7001 7002 7003 7004 7005; do
    mkdir -p /data/$port
    redis-server --port $port --dir /data/$port --cluster-enabled yes --cluster-config-file nodes.conf \
        --cluster-node-timeout 5000 --cluster-announce-ip 127.0.0.1 --appendonly yes \
        --daemonize yes --logfile /data/$port/redis.log
done
sleep 1
if ! redis-cli -p 7000 CLUSTER INFO | grep -q "cluster_state:ok"; then
    redis-cli --cluster create 127.0.0.1:7000 127.0.0.1:7001 127.0.0.1:7002 \
        127.0.0.1:7003 127.0.0.1:7004 127.0.0.1:7005 --cluster-replicas 1 --cluster-yes
fi
exec tail -F /data/7000/redis.log
