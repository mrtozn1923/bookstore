#!/usr/bin/env bash
# 12 - initiates the set, creates the users (localhost exception) and loads the Bookstore data
set -euo pipefail
if [ -z "${BOOKSTORE_APP_PASSWORD:-}" ]; then
    read -r -s -p "bookstore_app password: " BOOKSTORE_APP_PASSWORD
    echo
    export BOOKSTORE_APP_PASSWORD
fi
mongosh --quiet --port 27021 --eval '
    rs.initiate({ _id: "rs3", members: [
        { _id: 0, host: "localhost:27021" },
        { _id: 1, host: "localhost:27022" },
        { _id: 2, host: "localhost:27023" }
    ] }).ok'
until mongosh --quiet --port 27021 --eval 'db.hello().isWritablePrimary' | grep -q true; do sleep 1; done
mongosh --quiet --port 27021 admin --eval '
    db.createUser({ user: process.env.MONGO_ROOT_USER, pwd: process.env.MONGO_ROOT_PASSWORD, roles: ["root"] });
    db.auth(process.env.MONGO_ROOT_USER, process.env.MONGO_ROOT_PASSWORD);
    db.getSiblingDB("bookstore").createUser({ user: "bookstore_app", pwd: process.env.BOOKSTORE_APP_PASSWORD,
        roles: [{ role: "readWrite", db: "bookstore" }] }).ok'
for collection in categories authors books customers orders reviews; do
    mongoimport --uri "mongodb://bookstore_app@localhost:27021,localhost:27022,localhost:27023/bookstore?authSource=bookstore&replicaSet=rs3" \
        --password "$BOOKSTORE_APP_PASSWORD" --quiet \
        --collection "$collection" --drop --file "/lab/data/$collection.json"
done
mongosh --quiet --port 27021 -u "$MONGO_ROOT_USER" -p "$MONGO_ROOT_PASSWORD" --authenticationDatabase admin --eval '
    rs.status().members.forEach(m => print(m.name, m.stateStr));
    print("books:", db.getSiblingDB("bookstore").books.countDocuments())'
