#!/usr/bin/env bash
# 02 - loads the Bookstore data as bookstore_app (each collection is dropped first)
set -euo pipefail
if [ -z "${BOOKSTORE_APP_PASSWORD:-}" ]; then
    read -r -s -p "bookstore_app password: " BOOKSTORE_APP_PASSWORD
    echo
fi
for collection in categories authors books customers orders reviews; do
    mongoimport --uri "mongodb://bookstore_app@localhost:27017/bookstore?authSource=bookstore" \
        --password "$BOOKSTORE_APP_PASSWORD" \
        --collection "$collection" --drop --file "/lab/data/$collection.json" 2>&1 | grep -v "connected to"
done
