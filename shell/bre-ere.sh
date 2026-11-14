#!/usr/bin/env bash
line=$(tail -1 data/app.log)
# BRE: { } ( ) + ? | need a backslash; bare, they are literal characters
echo "$line" | grep -o 'BKS-[0-9]\{4\}-[0-9]\{6\}'
echo "$line" | grep -o 'BKS-[0-9]{4}' || echo "BRE {4}: no match"
# ERE: the same metacharacters without backslashes
echo "$line" | grep -oE 'BKS-[0-9]{4}-[0-9]{6}'
# sed is BRE by default, ERE with -E
echo "$line" | sed 's/.*Payment \([0-9]*\) \(declined\).*/\2: \1/'
echo "$line" | sed -E 's/.*Payment ([0-9]+) (declined).*/\2: \1/'
# ERE has no lookahead: grep warns and finds no match
echo "$line" | grep -oE 'Payment [0-9]+(?= declined)' || echo "ERE lookahead: no match"
