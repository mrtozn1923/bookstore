#!/usr/bin/env bash
# Checks every Terraform root and module without cloud credentials
set -euo pipefail
cd "$(dirname "$0")/../infra"

echo "== fmt"
terraform fmt -check -recursive

echo "== validate"
for dir in bootstrap-local bootstrap-azure local/envs/dev local/envs/staging azure/envs/dev azure/envs/staging azure/envs/prod; do
    terraform -chdir="$dir" init -backend=false -input=false -no-color >/dev/null
    printf '%-20s ' "$dir"
    terraform -chdir="$dir" validate -no-color
done

echo "== tflint"
tflint --init >/dev/null
tflint --recursive --config "$PWD/.tflint.hcl"

echo "== test (mock providers)"
for dir in modules/bookstore-webapp azure/bookstore; do
    terraform -chdir="$dir" init -input=false -no-color >/dev/null
    terraform -chdir="$dir" test -no-color
done
