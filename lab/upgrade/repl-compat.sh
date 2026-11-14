#!/bin/sh
# Starts a primary with one image and a replica with another; reports the replica's link status
docker network create bookstore-net >/dev/null
pair() {
  docker run -d --name bookstore-primary --network bookstore-net $1 >/dev/null
  docker run -d --name bookstore-replica --network bookstore-net $2 >/dev/null; sleep 2
  docker exec bookstore-primary redis-cli SET bookstore:order:id-seq 42 >/dev/null
  docker exec bookstore-replica redis-cli REPLICAOF bookstore-primary 6379 >/dev/null; sleep 9
  link=$(docker exec bookstore-replica redis-cli INFO replication | grep master_link_status | tr -d '\r')
  val=$(docker exec bookstore-replica redis-cli GET bookstore:order:id-seq)
  err=$(docker logs bookstore-replica 2>&1 | grep -iE "can.t handle|wrong signature|unexpected|failed" | grep -v -i config | tail -1 | cut -d' ' -f5-)
  printf '%-16s -> %-16s %s, GET=%s %s\n' "$1" "$2" "$link" "${val:-nil}" "$err"
  docker rm -f bookstore-primary bookstore-replica >/dev/null
}
pair redis:7-alpine redis:8
pair redis:7.2 valkey/valkey:9
pair redis:8 valkey/valkey:9
pair redis:8 redis:7-alpine
docker network rm bookstore-net >/dev/null
