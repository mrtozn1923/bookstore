#!/usr/bin/env bash
# Promote what runs in one environment to the next one through a branch (pull request).
# usage: CONFIG_REPO=<url or path> ci/promote.sh <api|web> <from-env> <to-env>
set -euo pipefail
component="$1"; from="$2"; to="$3"
config_repo="${CONFIG_REPO:?set CONFIG_REPO to the config repo URL}"
case "$from:$to" in
    dev:staging|staging:prod) ;;
    *) echo "promotion order is dev -> staging -> prod, not $from -> $to" >&2; exit 2 ;;
esac

work="$(mktemp -d)"; trap 'rm -rf "$work"' EXIT
git clone -q "$config_repo" "$work/config"
cd "$work/config"
# Exactly the images (tag@digest) that the source environment renders: nothing is rebuilt.
refs=$(kustomize build "apps/$component/overlays/$from" | grep -oE 'image: [^ ]+@sha256:[0-9a-f]+' | cut -d' ' -f2 | sort -u)
[ -n "$refs" ] || { echo "no digest-pinned image in $from" >&2; exit 1; }
version=$(echo "$refs" | head -1 | sed -E 's/.*:([^:@]+)@.*/\1/')
branch="promote/$to-$component-$version"
git switch -q -c "$branch"
cd "apps/$component/overlays/$to"
# localhost:52612/bookstore/api:1.2.0@sha256:... -> logical name bookstore/api
for ref in $refs; do kustomize edit set image "$(echo "$ref" | sed -E 's#^[^/]+/##; s/:[^:@]+@.*//')=$ref"; done
if git diff --quiet; then echo "$to already runs $version"; exit 0; fi
git add kustomization.yaml
git commit -q -m "$to: bookstore-$component $version" -m "Promoted-From: $from"
git push -q origin "$branch"
echo "==> pushed $branch; open a pull request: $branch -> main"
