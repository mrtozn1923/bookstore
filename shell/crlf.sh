#!/usr/bin/env bash
# a copy of the log with Windows line endings
crlf=$(mktemp)
sed 's/$/\r/' data/app.log > "$crlf"
tail -1 "$crlf" | od -c | tail -2
# $ is before \n, the \r is still part of the line
grep -c 'BKS-2026-000126$' data/app.log
grep -c 'BKS-2026-000126$' "$crlf"
grep -c $'BKS-2026-000126\r$' "$crlf"
# rg --crlf: $ also matches before \r\n
rg -c 'BKS-2026-000126$' "$crlf" || echo "rg: 0"
rg -c --crlf 'BKS-2026-000126$' "$crlf"
rm -f "$crlf"
