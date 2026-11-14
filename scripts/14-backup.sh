#!/usr/bin/env bash
# 14 - backup and a restore test: mongodump with --oplog, restored under another database name (admin)
set -euo pipefail
auth=(--host localhost --port 27017 --username "$MONGODB_INITDB_ROOT_USERNAME"
      --password "$MONGODB_INITDB_ROOT_PASSWORD" --authenticationDatabase admin)
archive=/tmp/bookstore-backup.gz

# Undo on exit: remove the archive and the restored copy
cleanup() {
    rm -f "$archive"
    mongosh --quiet "${auth[@]}" --eval 'db.getSiblingDB("bookstore_restore").dropDatabase()' > /dev/null
}
trap cleanup EXIT

mongodump "${auth[@]}" --oplog --gzip --archive="$archive" 2>&1 | grep oplog
echo "archive size: $(du -h "$archive" | cut -f1)"

mongorestore "${auth[@]}" --gzip --archive="$archive" \
    --nsInclude 'bookstore.*' --nsFrom 'bookstore.*' --nsTo 'bookstore_restore.*' 2>&1 | tail -1

mongosh --quiet "${auth[@]}" --eval '
    for (const name of ["books", "orders", "reviews", "customers"]) {
        const original = db.getSiblingDB("bookstore")[name];
        const restored = db.getSiblingDB("bookstore_restore")[name];
        print(name.padEnd(10), "documents", original.countDocuments(), "->", restored.countDocuments(),
              "| indexes", original.getIndexes().length, "->", restored.getIndexes().length);
    }'
