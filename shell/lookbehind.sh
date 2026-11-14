#!/usr/bin/env bash
# unbounded lookbehind: PCRE2 refuses it
grep -oP '(?<=price:\s*)[0-9]+' data/prices.txt
# bounded variable-length lookbehind: PCRE2 10.43+
grep -oP '(?<=price:\s{0,3})[0-9]+' data/prices.txt
# \K: forget what was matched so far
grep -oP 'price:\s*\K[0-9]+' data/prices.txt
# ripgrep's default engine has no lookaround, -P switches to PCRE2
rg -o '(?<=price: )[0-9]+' data/prices.txt
rg -oP '(?<=price:\s{0,3})[0-9]+' data/prices.txt
