#!/usr/bin/env bash
# Unicode script property: PCRE2 and ripgrep
grep -P '\p{Cyrillic}' data/titles.txt
rg '\p{Script=Cyrillic}' data/titles.txt
# POSIX ERE has no \p: the backslash is ignored with a warning
grep -E '\p{L}' data/titles.txt
# letter categories: one uppercase letter followed by lowercase letters
grep -P '^\p{Lu}\p{Ll}+$' data/titles.txt
rg '^\p{Lu}\p{Ll}+$' data/titles.txt
# \w with Turkish letters
printf 'Çalıkuşu\n' | grep -P '^\w+$'
printf 'Çalıkuşu\n' | rg '^\w+$'
