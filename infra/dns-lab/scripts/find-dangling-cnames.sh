#!/bin/sh
# Usage: find-dangling-cnames.sh <zone-name> <zone-file> <resolver>
zone=$1; file=$2; resolver=$3
dump=$(named-checkzone -D -o - "$zone" "$file" 2>/dev/null)
echo "$dump" | awk '$4 == "CNAME" {print $1, $5}' |
while read -r name target; do
    # targets inside our own zone are checked against the zone itself
    case "$target" in
        *"$zone.") echo "$dump" | awk -v t="$target" '$1 == t {f=1} END {exit !f}' && status=NOERROR || status=NXDOMAIN ;;
        *) status=$(dig @"$resolver" "$target" A +noall +comments | grep -o 'status: [A-Z]*' | cut -d' ' -f2) ;;
    esac
    asuid=$(echo "$dump" | awk -v n="asuid.$name" '$1 == n && $4 == "TXT" {print "yes"}')
    case "$status" in
        NOERROR) echo "OK        $name -> $target" ;;
        NXDOMAIN) echo "DANGLING  $name -> $target (verification TXT: ${asuid:-no})" ;;
        *) echo "CHECK     $name -> $target ($status)" ;;
    esac
done
