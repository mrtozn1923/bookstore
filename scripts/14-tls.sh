#!/usr/bin/env bash
# 14 - TLS: a lab CA, a server certificate for localhost and a second, temporary mongod that requires TLS
set -euo pipefail
dir=/tmp/tls
mkdir -p "$dir/db" && cd "$dir"
openssl req -x509 -newkey rsa:2048 -nodes -days 30 -subj "/CN=Bookstore Lab CA" -keyout ca.key -out ca.pem 2> /dev/null
openssl req -newkey rsa:2048 -nodes -subj "/CN=localhost" -addext "subjectAltName=DNS:localhost" \
    -keyout server.key -out server.csr 2> /dev/null
openssl x509 -req -in server.csr -CA ca.pem -CAkey ca.key -CAcreateserial -days 30 \
    -copy_extensions copy -out server.crt 2> /dev/null
cat server.key server.crt > server.pem

# Undo on exit: stop the temporary mongod and remove its files
cleanup() {
    mongod --shutdown --dbpath "$dir/db" > /dev/null 2>&1 || true
    rm -rf "$dir"
}
trap cleanup EXIT

mongod --port 27040 --dbpath "$dir/db" --tlsMode requireTLS --tlsCertificateKeyFile server.pem --tlsCAFile ca.pem \
    --tlsAllowConnectionsWithoutCertificates --logpath "$dir/mongod.log" --fork > /dev/null

echo "--- without TLS"
mongosh --quiet "mongodb://localhost:27040/?serverSelectionTimeoutMS=3000" --eval 'db.hello().ok' 2>&1 | head -1 || true
echo "--- with TLS, without trusting the lab CA"
mongosh --quiet "mongodb://localhost:27040/?tls=true&serverSelectionTimeoutMS=3000" --eval 'db.hello().ok' 2>&1 | head -1 || true
echo "--- with TLS and the lab CA"
mongosh --quiet "mongodb://localhost:27040/?tls=true&tlsCAFile=ca.pem" --eval '
    print("hello ok:", db.hello().ok);
    printjson(db.serverStatus().transportSecurity)'
