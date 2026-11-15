#!/usr/bin/env bash
# Push-based deploy: what a CI job does when it runs kubectl itself.
# It needs a kubeconfig with write access to the cluster.
set -euo pipefail
overlay="${1:-apps/api/overlays/dev}"
kubectl apply -k "$overlay"
kubectl -n bookstore-dev rollout status deployment/api --timeout=120s
