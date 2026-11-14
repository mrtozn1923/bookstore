#!/usr/bin/env bash
# run every probe with grep -E, grep -P and rg: first matched text, "-" (no match),
# "warning" (warned and found nothing) or "error"
cell() {
    local out err
    out=$(printf '%s\n' "$2" | "${@:3}" -o -e "$1" 2>/dev/null | head -1)
    err=$(printf '%s\n' "$2" | "${@:3}" -o -e "$1" 2>&1 >/dev/null)
    if [[ -n $err && -z $out ]]; then
        [[ $err == *warning:* ]] && out=warning || out=error
    fi
    out=${out:--}
    printf '%s%*s' "$out" $((17 - ${#out})) ''
}
printf '%-14s%-17s%-17s%s\n' probe grep-E grep-P rg
while IFS=$'\t' read -r name pattern input; do
    printf '%-14s' "$name"
    cell "$pattern" "$input" grep -E
    cell "$pattern" "$input" grep -P
    cell "$pattern" "$input" rg
    echo
done < data/flavor-probes.tsv | sed "s/ *$//"
