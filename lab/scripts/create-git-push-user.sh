#!/usr/bin/env bash
# Creates the only user that may push to the lab git server (Argo CD Image Updater, post 06).
# The password is generated here and stored only in two Secrets:
#   git-server/git-server-auth      (htpasswd for nginx)
#   argocd/repo-bookstore-config    (an Argo CD repository Secret; Image Updater's "repocreds")
set -euo pipefail
user="image-updater"
password="$(openssl rand -hex 16)"
repo="http://git-server.git-server.svc.cluster.local/bookstore-config"
kubectl create namespace git-server --dry-run=client -o yaml | kubectl apply -f - >/dev/null
kubectl -n git-server create secret generic git-server-auth \
    --from-literal=htpasswd="$user:$(openssl passwd -apr1 "$password")" \
    --dry-run=client -o yaml | kubectl apply -f -
kubectl -n argocd create secret generic repo-bookstore-config \
    --from-literal=type=git --from-literal=url="$repo" \
    --from-literal=username="$user" --from-literal=password="$password" \
    --dry-run=client -o yaml | kubectl label --local -f - argocd.argoproj.io/secret-type=repository -o yaml | kubectl apply -f -
