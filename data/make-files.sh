#!/bin/bash
# Builds the publisher price list files used in post 23 from the lab database (needs bcp and python3).
set -euo pipefail
bcp "SELECT Isbn, CAST(Price AS varchar(20)) FROM dbo.Books ORDER BY BookId" queryout books.tsv -c -t "\t" -d sqls_23 -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -u
mkdir -p files
python3 gen_prices.py files books.tsv
sed '8s/"18,05"/18,05/' files/prices.csv > files/prices_v1.csv
