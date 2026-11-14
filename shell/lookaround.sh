#!/usr/bin/env bash
# PCRE2: lookaround works in grep -P
grep -P '^(?!.* DBG ).*Bookstore\.Payments' data/app.log
echo
# POSIX ERE has no lookaround: two greps in a pipe do the same job
grep -E 'Bookstore\.Payments' data/app.log | grep -v ' DBG '
echo
# \K drops what was matched so far from the output
grep -oP 'Payment \K[0-9]+(?= declined)' data/app.log
