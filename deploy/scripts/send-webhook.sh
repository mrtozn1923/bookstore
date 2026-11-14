#!/bin/bash
# send-webhook.sh SECRETFILE : signs a payment event with that secret and posts it
BODY='{"orderId":"A-1001","status":"paid","amount":389.00}'
SIG=$(printf '%s' "$BODY" | openssl dgst -sha256 -hmac "$(tr -d '\n' < $1)" -hex | awk '{print $NF}')
curl -s -o /dev/null -w "%{http_code}\n" -X POST localhost:5097/api/payments/webhook -H "Content-Type: application/json" -H "X-Bookstore-Signature: sha256=$SIG" --data "$BODY"
