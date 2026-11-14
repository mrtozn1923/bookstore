#!/bin/sh
# Post 13: one primary, one replica and three Sentinels in a single container (lab only, never like this in production)
set -e
mkdir -p /data/sentinel
redis-server --port 6390 --dir /data --dbfilename primary.rdb --replica-announce-ip 127.0.0.1 \
    --daemonize yes --logfile /data/primary.log
redis-server --port 6391 --dir /data --dbfilename replica.rdb --replicaof 127.0.0.1 6390 --replica-announce-ip 127.0.0.1 \
    --daemonize yes --logfile /data/replica.log
for port in 26379 26380 26381; do
    cat > /data/sentinel/$port.conf <<CONF
port $port
sentinel announce-ip 127.0.0.1
sentinel monitor bookstore 127.0.0.1 6390 2
sentinel down-after-milliseconds bookstore 5000
sentinel failover-timeout bookstore 10000
CONF
    redis-sentinel /data/sentinel/$port.conf --daemonize yes --logfile /data/sentinel-$port.log
done
exec tail -F /data/sentinel-26379.log
