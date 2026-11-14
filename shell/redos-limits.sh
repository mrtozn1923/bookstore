#!/usr/bin/env bash
# PCRE2's "minimum match limit": how many internal match steps a failing input needs
# limit PATTERN UNIT COUNT TAIL: the subject is UNIT repeated COUNT times, then TAIL
limit() {
    local subject
    subject=$(printf "%${3}s" '' | sed "s/ /$2/g")$4
    printf '/%s/\n%s\\=find_limits\n' "$1" "$subject" > /tmp/limits.txt
    pcre2test /tmp/limits.txt | sed -n 's/^Minimum match limit = //p'
}
printf '%-28s %8s %8s %10s\n' pattern n=10 n=15 n=20
while IFS=$'\t' read -r pattern unit tail; do
    printf '%-28s %8s %8s %10s\n' "$pattern" "$(limit "$pattern" "$unit" 10 "$tail")" \
        "$(limit "$pattern" "$unit" 15 "$tail")" "$(limit "$pattern" "$unit" 20 "$tail")"
done <<'PATTERNS'
^(a+)+$	a	!
^(\w+\s?)*$	a	!
^(\d|\d\d)+$	1	x
^.*.*=.*;$	x	=;x
^(?>a+)+$	a	!
^a+$	a	!
^\w+(?:\s\w+)*$	a	!
PATTERNS
rm -f /tmp/limits.txt
# grep -P stops at PCRE2's default limit instead of hanging
printf '%040d!\n' 0 | tr 0 a | grep -cP '^(a+)+$'
