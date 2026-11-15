#!/bin/bash
# Runs where the files are (bcp is a client tool). Needs MSSQL_SA_PASSWORD.
set -euo pipefail
bcp sqls_23.dbo.PriceListStaging format /dev/null -c -t, -r 0x0a -f prices.fmt -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -u
bcp "SELECT Isbn, Title, Price FROM dbo.Books WHERE Category = 'Classic'" queryout classics.tsv -c -t "\t" -d sqls_23 -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -u -b 5000
bcp dbo.ClassicsCopy in classics.tsv -c -t "\t" -d sqls_23 -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -u -b 5000 -h "TABLOCK"
