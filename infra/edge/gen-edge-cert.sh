#!/bin/sh
# Self-signed certificate for the lab edge (never commit the key).
openssl req -x509 -newkey ec -pkeyopt ec_paramgen_curve:P-256 -nodes -days 30 \
    -subj "/CN=bookstore.example" -addext "subjectAltName=DNS:bookstore.example" \
    -keyout edge.key -out edge.crt
