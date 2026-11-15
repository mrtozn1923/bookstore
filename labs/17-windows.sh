#!/usr/bin/env bash
# Docker series 17: the parts of the Windows containers post that run on a Linux engine (registry reads,
# manifest lists, linting). Building and running the Windows image needs a Windows host: see the post.
# Run from the repository root. Needs jq; uses a local registry on 127.0.0.1:52817.
set -uo pipefail

echo "### Windows variants behind one .NET Framework tag"
docker buildx imagetools inspect mcr.microsoft.com/dotnet/framework/aspnet:4.8.1
echo "### Pulling it on a Linux engine"
docker pull mcr.microsoft.com/dotnet/framework/aspnet:4.8.1
echo "### Compressed sizes (registry metadata only)"
while read -r image platform; do
    bash labs/17-windows/image-sizes.sh "$image" "$platform"
done <<EOF
mcr.microsoft.com/windows/nanoserver:ltsc2025 windows/amd64
mcr.microsoft.com/windows/servercore:ltsc2025 windows/amd64
mcr.microsoft.com/dotnet/framework/aspnet:4.8.1-windowsservercore-ltsc2025 windows/amd64
mcr.microsoft.com/dotnet/framework/sdk:4.8.1-windowsservercore-ltsc2025 windows/amd64
mcr.microsoft.com/dotnet/aspnet:10.0-nanoserver-ltsc2025 windows/amd64
mcr.microsoft.com/dotnet/aspnet:10.0 linux/amd64
mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra linux/amd64
EOF

echo "### One tag, several operating systems"
docker buildx imagetools inspect golang:1.25 \
    --format '{{range .Manifest.Manifests}}{{with .Platform}}{{.OS}}/{{.Architecture}} {{.OSVersion}}{{end}}{{"\n"}}{{end}}' \
    | grep -v unknown | grep .

echo "### Same tag pushed twice, then one manifest list"
docker run -d --name dockerseries-registry -p 127.0.0.1:52817:5000 registry:3
sleep 2
R=127.0.0.1:52817/bookstore/api
platforms() {
    docker buildx imagetools inspect "$1" \
        --format '{{range .Manifest.Manifests}}{{with .Platform}}{{.OS}}/{{.Architecture}}{{end}}{{"\n"}}{{end}}' | grep -v unknown | grep .
}
for arch in amd64 arm64; do
    docker build -q --platform linux/$arch -f src/Bookstore.Api/Dockerfile -t $R:1.3.0 .
    docker push -q $R:1.3.0
done
platforms $R:1.3.0
for arch in amd64 arm64; do
    docker build -q --platform linux/$arch -f src/Bookstore.Api/Dockerfile -t $R:1.3.0-linux-$arch .
    docker push -q $R:1.3.0-linux-$arch
done
docker buildx imagetools create -t $R:1.3.0 $R:1.3.0-linux-amd64 $R:1.3.0-linux-arm64
platforms $R:1.3.0
docker rm -f dockerseries-registry

echo "### Linux-side linting of the Windows Dockerfile"
docker build --check --platform linux/amd64 -f legacy/Bookstore.BackOffice/Dockerfile .
docker run --rm -i hadolint/hadolint:v2.15.1 hadolint --no-color - < legacy/Bookstore.BackOffice/Dockerfile

echo "### The Kubernetes manifest (schema only)"
kubeconform -strict -summary -kubernetes-version 1.35.0 labs/17-windows/backoffice-windows.yaml
