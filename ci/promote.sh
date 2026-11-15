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
# Exactly the images (tag@digest) of the source environment: nothing is rebuilt.
# Read from the overlay's images list, not from `kustomize build`: an overlay with a KSOPS
# generator (post 07) would need the decryption key just to render.
refs=$(awk '/^images:/ {on=1; next} on && /^[^ -]/ {on=0}
    on && /^- / {if (n) print n "=" nn ":" t "@" d; d = n = nn = t = ""; sub(/^- /, "  ")}
    on && $1 == "digest:" {d = $2} on && $1 == "name:" {n = $2} on && $1 == "newName:" {nn = $2} on && $1 == "newTag:" {t = $2}
    END {if (n) print n "=" nn ":" t "@" d}' "apps/$component/overlays/$from/kustomization.yaml" | grep '@sha256:')
[ -n "$refs" ] || { echo "no digest-pinned image in $from" >&2; exit 1; }
version=$(echo "$refs" | head -1 | sed -E 's/.*:([^:@]+)@.*/\1/')
branch="promote/$to-$component-$version"
git switch -q -c "$branch"
cd "apps/$component/overlays/$to"
for ref in $refs; do kustomize edit set image "$ref"; done
if git diff --quiet; then echo "$to already runs $version"; exit 0; fi
git add kustomization.yaml
git commit -q -m "$to: bookstore-$component $version" -m "Promoted-From: $from"
git push -q origin "$branch"
echo "==> pushed $branch; open a pull request: $branch -> main"
