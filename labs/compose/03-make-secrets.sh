#!/usr/bin/env bash
# Creates the local development secrets under secrets/ (git- and docker-ignored). Run once per machine.
set -euo pipefail
cd "$(dirname "$0")/../.."
mkdir -p secrets
new_password() { printf '%s' "Bk$(openssl rand -base64 18 | tr -d '/+=')!9"; }

[ -f secrets/sa-password.txt ] || new_password > secrets/sa-password.txt
[ -f secrets/db-app-password.txt ] || new_password > secrets/db-app-password.txt
printf 'Server=db;Database=BookstoreDb;User Id=bookstore_app;Password=%s;TrustServerCertificate=True' \
    "$(cat secrets/db-app-password.txt)" > secrets/api-connection-string.txt
# The containers run as other users (SQL Server 10001, Bookstore.Api 1654): the files must be readable for them
chmod 644 secrets/*.txt
ls -l secrets
