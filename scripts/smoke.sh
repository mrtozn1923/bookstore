#!/usr/bin/env bash
# Bookstore smoke test: login -> books -> cart -> order -> read the order.
# Usage: BOOKSTORE_PASSWORD=... ./scripts/smoke.sh   (BOOKSTORE_URL and BOOKSTORE_EMAIL are optional)
# Needs curl 8.3+ (--variable) and jq.
set -euo pipefail

base="${BOOKSTORE_URL:-http://localhost:5080}"
email="${BOOKSTORE_EMAIL:-ada@example.com}"
password="${BOOKSTORE_PASSWORD:?Set BOOKSTORE_PASSWORD}"

common=(--silent --show-error --fail-with-body --max-time 10)
# only for requests that are safe to repeat
retry=(--retry 3 --retry-connrefused --retry-delay 1)
auth=(--variable %BOOKSTORE_TOKEN --expand-header "Authorization: Bearer {{BOOKSTORE_TOKEN}}")

# call <step> <curl arguments...>: prints the body, or the error body and exits on HTTP >= 400
call() {
    local step=$1 body
    shift
    if ! body=$(curl "${common[@]}" "$@"); then
        echo "FAIL  $step: $body" >&2
        exit 1
    fi
    printf '%s' "$body"
}

login_body=$(jq -n --arg email "$email" --arg password "$password" '{email: $email, password: $password}')
BOOKSTORE_TOKEN=$(call login "${retry[@]}" "$base/api/auth/login" --json "$login_body" | jq -r .accessToken)
export BOOKSTORE_TOKEN
echo "ok    login as $email"

isbn=$(call books "${retry[@]}" "$base/api/books?page=1&search=Hobbit" | jq -r '.items[0].isbn')
echo "ok    book $isbn"

cart=$(call cart "${auth[@]}" -X POST "$base/api/carts" | jq -r .id)
total=$(call add-item "${auth[@]}" -X PUT "$base/api/carts/$cart/items/$isbn" --json '{"quantity":2}' | jq -r .total)
echo "ok    cart $cart total $total"

order_body=$(jq -n --arg cart "$cart" --arg email "$email" '{cartId: $cart, customerName: "Smoke Test", customerEmail: $email}')
location=$(call order "${auth[@]}" "$base/api/orders" --json "$order_body" -o /dev/null -w '%header{location}')
echo "ok    order $location"

status=$(call read-order "${auth[@]}" "${retry[@]}" "$location" | jq -r .status)
if [[ "$status" != "Placed" ]]; then
    echo "FAIL  read-order: expected Placed, got $status" >&2
    exit 1
fi
echo "ok    status $status"
