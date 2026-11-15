#!/usr/bin/env bash
# Lab registry for post 06: a registry:3 container next to the kind node.
# Images are pushed from the host as localhost:52612/bookstore/<name>; the node pulls the same names
# from the container (containerd hosts.toml), so manifests and CI use one image name.
set -euo pipefail
cluster="${KIND_CLUSTER:-gitopsseries-1}"
network="$(docker inspect -f '{{range $k, $v := .NetworkSettings.Networks}}{{$k}}{{end}}' "$cluster-control-plane")"
docker run -d --name gitopsseries-registry --network "$network" -p 127.0.0.1:52612:5000 registry:3
docker exec "$cluster-control-plane" mkdir -p /etc/containerd/certs.d/localhost:52612
printf '[host."http://gitopsseries-registry:5000"]\n' |
    docker exec -i "$cluster-control-plane" cp /dev/stdin /etc/containerd/certs.d/localhost:52612/hosts.toml
