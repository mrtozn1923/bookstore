#!/usr/bin/env bash
# Decides the Bookstore.Contracts package version: a stable version only when a run on main is started
# with publishStable=true, otherwise a prerelease that is unique per run (feeds never accept the same version twice).
set -euo pipefail

project="src/Bookstore.Contracts/Bookstore.Contracts.csproj"
prefix=$(sed -n 's:.*<VersionPrefix>\(.*\)</VersionPrefix>.*:\1:p' "$project")
branch="${BUILD_SOURCEBRANCH:-refs/heads/local}"
build_id="${BUILD_BUILDID:-0}"
stable="${PUBLISH_STABLE:-false}"

if [[ "$stable" == "True" || "$stable" == "true" ]]; then
  if [[ "$branch" != "refs/heads/main" ]]; then
    echo "##vso[task.logissue type=error]Stable contracts versions are published only from main (this run: ${branch})."
    exit 1
  fi
  version="${prefix}"
else
  version="${prefix}-ci.${build_id}"
fi

echo "Contracts package version: ${version}"
echo "##vso[task.setvariable variable=contractsVersion]${version}"
