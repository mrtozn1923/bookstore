#!/usr/bin/env bash
# PCRE2 caseless matching follows Unicode, not Turkish rules
grep -iP 'ince' data/catalog-tr.txt
# (*TURKISH_CASING) at the start of the pattern: PCRE2 10.45+
grep -iP '(*TURKISH_CASING)ince' data/catalog-tr.txt
grep -iP '(*TURKISH_CASING)ıssız' data/catalog-tr.txt
# GNU grep -E uses the locale's case rules
LC_ALL=tr_TR.UTF-8 grep -iE 'ince' data/catalog-tr.txt
LC_ALL=en_US.UTF-8 grep -iE 'ince' data/catalog-tr.txt
