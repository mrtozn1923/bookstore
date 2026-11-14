#!/usr/bin/env bash
# default engine (Rust regex): no lookaround, it reports an error
rg -o 'Payment [0-9]+(?= declined)' data/app.log
# -P for PCRE2, --engine auto for "PCRE2 when needed"
rg -oP 'Payment [0-9]+(?= declined)' data/app.log
rg -o --engine auto 'Payment [0-9]+(?= declined)' data/app.log
# syntax that is accepted but means something else
printf 'f(a(b)c)\n' | rg -o '\((?:[^()]|(?R))*\)'
printf 'f(a(b)c)\n' | rg -oP '\((?:[^()]|(?R))*\)'
printf '1230\n' | rg -o '[0-9]++0'
printf '1230\n' | rg -oP '[0-9]++0' || echo "PCRE2 possessive: no match"
