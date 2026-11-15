#!/usr/bin/env bash
# Creates the api-db Secret (PostgreSQL password + connection string) in a namespace.
# The password is generated here and never written to Git (secrets in GitOps: post 07).
# usage: ./create-db-secret.sh bookstore-dev
set -euo pipefail
ns="$1"
kubectl create namespace "$ns" --dry-run=client -o yaml | kubectl apply -f - >/dev/null
if kubectl -n "$ns" get secret api-db >/dev/null 2>&1; then
    echo "secret/api-db already exists in $ns"
    exit 0
fi
password="$(openssl rand -hex 16)"
kubectl -n "$ns" create secret generic api-db \
    --from-literal=password="$password" \
    --from-literal=connection-string="Host=postgres;Database=bookstore;Username=bookstore;Password=$password"
