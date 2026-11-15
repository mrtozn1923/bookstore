#!/usr/bin/env bash
# Computes the application version once per run and publishes it as an output variable.
# Version policy (SemVer, changelog) is out of scope here; this only adds a branch-based suffix.
set -euo pipefail

prefix=$(sed -n 's:.*<VersionPrefix>\(.*\)</VersionPrefix>.*:\1:p' Directory.Build.props)
branch="${BUILD_SOURCEBRANCH:-refs/heads/local}"
build_id="${BUILD_BUILDID:-0}"

case "$branch" in
  refs/heads/main)      version="${prefix}-main.${build_id}" ;;
  refs/heads/release/*) version="${prefix}-rc.${build_id}" ;;
  *)                    version="${prefix}-ci.${build_id}" ;;
esac

echo "App version: ${version}"
echo "##vso[task.setvariable variable=appVersion;isOutput=true]${version}"
