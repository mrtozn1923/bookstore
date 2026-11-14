#!/usr/bin/env bash
# -o only the match, -i case-insensitive, -w whole words only
grep -oiwE 'payment [0-9]+' data/app.log
echo '---'
# -r recursive, -n line number, --include file filter
grep -rnE --include='*.cs' 'Log[A-Za-z]+\(\$"' sample
echo '---'
# -l only file names, -c count per file
grep -rcE 'Log[A-Za-z]+\(\$"' sample
echo '---'
# rg: recursive by default, -t cs file type, --sort path stable order
rg -n --sort path -t cs 'Log\w+\(\$"' sample
echo '---'
# -g glob filter: everything except Bookstore.Ordering
rg -l --sort path -g '!**/Bookstore.Ordering/**' 'Log\w+\(\$"' sample
echo '---'
# -U: the pattern may span lines (\s also matches \n)
rg -n -U 'Log\w+\(\s*\$"' sample/Bookstore.Payments
