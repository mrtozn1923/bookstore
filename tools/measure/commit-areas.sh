#!/usr/bin/env bash
# Post 15: which service folders every commit touched. Commits that touch many services at once are the
# "change together" symptom of a distributed monolith.
set -euo pipefail
git log --reverse --format='%h %s' | while read -r hash subject; do
  areas=$(git diff-tree --root --no-commit-id --name-only -r "$hash" \
    | awk -F/ '$1=="src" && $2=="Services" {print $3} $1=="src" && $2=="BuildingBlocks" {print "BuildingBlocks"} $1=="Directory.Packages.props" {print "packages"}' \
    | sort -u | tr '\n' ' ')
  echo "${subject:0:40} | ${areas}"
done
