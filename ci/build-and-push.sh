#!/usr/bin/env bash
# Local CI for the GitOps series: build once, push to the registry, promote to dev with a commit.
# usage: CONFIG_REPO=<url or path> ci/build-and-push.sh <api|web> <version>
#        PUSH_ONLY=1 ci/build-and-push.sh ...   (images only: Argo CD Image Updater updates dev)
#        FAULT_RATE=0.3 ...                      (lab only: a deliberately failing image, post 08)
set -euo pipefail
component="$1"; version="$2"
registry="${REGISTRY:-localhost:52612}"
source_commit="$(git rev-parse --short=12 HEAD)"

case "$component" in
    api) images=("api:src/Bookstore.Api/Dockerfile" "migrator:src/Bookstore.Migrations.Postgres/Dockerfile"); context=. ;;
    web) images=("web:Dockerfile"); context=web/bookstore-web ;;
    *) echo "unknown component: $component" >&2; exit 2 ;;
esac

refs=()
for entry in "${images[@]}"; do
    name="${entry%%:*}"; dockerfile="${entry#*:}"
    image="$registry/bookstore/$name"
    echo "==> build $image:$version (source $source_commit)"
    docker build -q -f "$context/$dockerfile" -t "$image:$version" \
        --build-arg APP_VERSION="$version" --build-arg SOURCE_COMMIT="$source_commit" --build-arg FAULT_RATE="${FAULT_RATE:-0}" \
        --label org.opencontainers.image.revision="$source_commit" \
        --label org.opencontainers.image.version="$version" "$context" >/dev/null
    docker push -q "$image:$version" >/dev/null
    # The digest the registry stored; Git gets tag AND digest, the digest is what the node pulls.
    digest="$(docker buildx imagetools inspect "$image:$version" --format '{{json .Manifest.Digest}}' | tr -d '"')"
    # Manifests use the logical name bookstore/<name>; the overlay maps it to registry, tag and digest.
    refs+=("bookstore/$name=$image:$version@$digest")
    echo "    pushed $image:$version@$digest"
done

[ "${PUSH_ONLY:-0}" = 1 ] && exit 0

# Promote to dev: a commit in the config repo, never kubectl.
config_repo="${CONFIG_REPO:?set CONFIG_REPO to the config repo URL}"
work="$(mktemp -d)"; trap 'rm -rf "$work"' EXIT
git clone -q "$config_repo" "$work/config"
cd "$work/config/apps/$component/overlays/dev"
for ref in "${refs[@]}"; do kustomize edit set image "$ref"; done
git add kustomization.yaml
git commit -q -m "dev: bookstore-$component $version" -m "Source-Commit: $source_commit"
for attempt in 1 2 3; do
    if git push -q origin HEAD:main; then
        echo "==> config repo: $(git log -1 --format="%h %s")"
        exit 0
    fi
    echo "    push rejected (attempt $attempt), rebasing on the new main"
    git pull -q --rebase origin main
done
echo "could not push after 3 attempts" >&2
exit 1
