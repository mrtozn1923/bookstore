#!/usr/bin/env bash
# Writes a SealedSecret for the api-db Secret of one environment (post 07), sealed for YOUR cluster.
# The password is generated here and exists in plain text only inside this pipe.
# usage: ./seal-db-secret.sh bookstore-dev > ../../gitops/apps/api/overlays/dev/sealed-api-db.yaml
set -euo pipefail
ns="$1"
password="$(openssl rand -hex 16)"
kubectl create secret generic api-db --namespace "$ns" \
    --from-literal=password="$password" \
    --from-literal=connection-string="Host=postgres;Database=bookstore;Username=bookstore;Password=$password" \
    --dry-run=client -o yaml |
    kubeseal --format yaml
