#!/usr/bin/env bash
# how many steps does PCRE2 report for each pattern on one log line?
line=${1:-$(head -1 data/access.log)}
input=$(mktemp)
for p in '".*"' '".*?"' '"[^"]*"' '^.*" ([0-9]{3}) ' '^.*?" ([0-9]{3}) ' '^[^"]*"[^"]*" ([0-9]{3}) '; do
    printf '/%s/auto_callout%s\n%s\n' "$p" "${OPTS:-}" "$line" > "$input"
    out=$(pcre2test "$input")
    steps=$(grep -c '^ *+[0-9]' <<< "$out")
    match=$(grep -m1 '^ 0:' <<< "$out" | cut -c5-)
    printf '%-26s %4s  %s\n' "$p" "$steps" "${match:-no match}"
done
rm -f "$input"
