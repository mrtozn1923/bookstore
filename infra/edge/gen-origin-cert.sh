#!/bin/sh
# Self-signed certificate for the lab origin (Kestrel). Never commit the key.
# Run the origin with:
#   Kestrel__Certificates__Default__Path=origin.crt Kestrel__Certificates__Default__KeyPath=origin.key \
#   dotnet run --project src/Bookstore.Api --urls https://127.0.0.1:5443
openssl req -x509 -newkey rsa:2048 -nodes -days 30 \
    -subj "/CN=origin.bookstore.example" -addext "subjectAltName=DNS:origin.bookstore.example" \
    -keyout origin.key -out origin.crt
