#!/usr/bin/env bash
# Lab only: random sa password -> Secret mssql (bookstore-db) and the API's connection string Secret
# (bookstore-db-connection) in every namespace given as argument. Nothing is written to disk or Git.
set -euo pipefail
kubectl get ns bookstore-db >/dev/null 2>&1 || kubectl create ns bookstore-db
if ! kubectl -n bookstore-db get secret mssql >/dev/null 2>&1; then
    pw="Lab-$(openssl rand -hex 12)-Aa1"
    kubectl -n bookstore-db create secret generic mssql --from-literal=sa-password="$pw"
fi
pw=$(kubectl -n bookstore-db get secret mssql -o jsonpath='{.data.sa-password}' | base64 -d)
for ns in "$@"; do
    kubectl get ns "$ns" >/dev/null 2>&1 || kubectl create ns "$ns"
    kubectl -n "$ns" create secret generic bookstore-db-connection \
        --from-literal=connection-string="Server=mssql.bookstore-db.svc.cluster.local,1433;Database=BookstoreDb;User Id=sa;Password=$pw;TrustServerCertificate=True" \
        --dry-run=client -o yaml | kubectl apply -f -
done
