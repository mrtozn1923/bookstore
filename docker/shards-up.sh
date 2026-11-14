#!/bin/sh
# Two independent PostgreSQL 18 servers used as shards in post 09 (buckets 0-31 -> shard0, 32-63 -> shard1).
# Set POSTGRES_PASSWORD in your shell first; run from the snapshot root.
set -e
for i in 0 1; do
  docker run -d --name bookstore-shard$i -p $((5440 + i)):5432 -e POSTGRES_PASSWORD="$POSTGRES_PASSWORD" -e POSTGRES_DB=bookstore postgres:18
done
sleep 8
for i in 0 1; do
  for f in pg-schema pg-data pg-indexes pg-shard-bucket; do docker exec -i bookstore-shard$i psql -q -U postgres -d bookstore < db/$f.sql; done
  docker exec bookstore-shard$i psql -q -U postgres -d bookstore -c "DELETE FROM orders WHERE (bucket_of(customer_id) >= 32) <> ($i = 1)" \
    -c "DELETE FROM customers WHERE (bucket_of(customer_id) >= 32) <> ($i = 1)" -c "VACUUM ANALYZE"
done
