#!/usr/bin/env bash
# 13 - users (localhost exception on mongos), the two shards and the Bookstore data
set -euo pipefail
if [ -z "${BOOKSTORE_APP_PASSWORD:-}" ]; then
    read -r -s -p "bookstore_app password: " BOOKSTORE_APP_PASSWORD
    echo
    export BOOKSTORE_APP_PASSWORD
fi
until mongosh --quiet --port 27030 --eval 'db.hello().ok' 2>/dev/null | grep -q 1; do sleep 1; done
mongosh --quiet --port 27030 admin --eval '
    db.createUser({ user: process.env.MONGO_ROOT_USER, pwd: process.env.MONGO_ROOT_PASSWORD, roles: ["root"] });
    db.auth(process.env.MONGO_ROOT_USER, process.env.MONGO_ROOT_PASSWORD);
    sh.addShard("shardA/localhost:27032");
    sh.addShard("shardB/localhost:27033");
    db.getSiblingDB("bookstore").createUser({ user: "bookstore_app", pwd: process.env.BOOKSTORE_APP_PASSWORD,
        roles: [{ role: "readWrite", db: "bookstore" }] });
    db.getSiblingDB("config").shards.find().forEach(s => print(s._id, s.host))'
for collection in categories authors books customers orders reviews; do
    mongoimport --uri "mongodb://bookstore_app@localhost:27030/bookstore?authSource=bookstore" \
        --password "$BOOKSTORE_APP_PASSWORD" --quiet \
        --collection "$collection" --drop --file "/lab/data/$collection.json"
done
