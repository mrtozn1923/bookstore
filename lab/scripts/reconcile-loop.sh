#!/usr/bin/env bash
# A toy pull loop: the core idea of a GitOps agent in a few lines. Not for production.
# usage: ./reconcile-loop.sh <git-clone> <kustomize-path> [interval-seconds]
set -uo pipefail
clone="$1"; path="$2"; interval="${3:-30}"
while true; do
    git -C "$clone" pull --ff-only -q
    rev="$(git -C "$clone" rev-parse --short HEAD)"
    kubectl diff --server-side --force-conflicts -k "$clone/$path" > /dev/null 2>&1
    case $? in
        0) echo "$(date +%T) $rev in sync" ;;
        1) echo "$(date +%T) $rev drift detected, applying"
           kubectl apply --server-side --force-conflicts -k "$clone/$path" | grep -v unchanged ;;
        *) echo "$(date +%T) $rev diff failed" >&2 ;;
    esac
    sleep "$interval"
done
