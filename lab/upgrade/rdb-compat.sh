#!/bin/sh
# Writes an RDB with one image and loads it with another; prints the RDB header and the result
W="$(pwd)/rdbtest"
pair() {
  src=$1; dst=$2; rm -rf $W; mkdir -p $W; chmod 777 $W
  docker run -d --name bookstore-rdb-src -v $W:/data $src >/dev/null; sleep 2
  docker exec bookstore-rdb-src redis-cli SET bookstore:order:id-seq 42 >/dev/null
  docker exec bookstore-rdb-src redis-cli HSET bookstore:cart:7 9780441013593 2 >/dev/null
  docker exec bookstore-rdb-src redis-cli XADD bookstore:orders:events '*' type order-created >/dev/null
  docker exec bookstore-rdb-src redis-cli SAVE >/dev/null
  docker rm -f bookstore-rdb-src >/dev/null
  header=$(head -c 9 $W/dump.rdb)
  docker run -d --name bookstore-rdb-dst -v $W:/data $dst >/dev/null; sleep 3
  if [ "$(docker inspect -f '{{.State.Running}}' bookstore-rdb-dst)" = "true" ]; then
    result="loaded, DBSIZE $(docker exec bookstore-rdb-dst redis-cli DBSIZE)"
  else
    result="FAILED: $(docker logs bookstore-rdb-dst 2>&1 | grep -E '#' | grep -v -i 'config' | head -1 | cut -d' ' -f5-)"
  fi
  docker rm -f bookstore-rdb-dst >/dev/null
  printf '%-16s -> %-16s %s  %s\n' "$src" "$dst" "$header" "$result"
}
pair redis:7.2 redis:8
pair redis:7-alpine redis:8
pair redis:8 redis:7-alpine
pair redis:7.2 valkey/valkey:9
pair redis:7-alpine valkey/valkey:9
pair redis:8 valkey/valkey:9
pair valkey/valkey:9 redis:8
pair valkey/valkey:9 redis:7.2
rm -rf $W
