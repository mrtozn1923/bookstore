#!/bin/sh
# Usage: check-delegation.sh <domain> <parent-nameserver>
domain=$1; parent=$2
referral=$(dig @"$parent" "$domain" NS +norec +noall +authority +additional)
parent_ns=$(echo "$referral" | awk '$4 == "NS" {print tolower($5)}' | sort)
echo "parent ($parent) says: $(echo $parent_ns)"
for ns in $parent_ns; do
    # use the glue address if the parent sent one, otherwise the system resolver
    addr=$(echo "$referral" | awk -v n="$ns" 'tolower($1) == n && $4 == "A" {print $5; exit}')
    [ -n "$addr" ] || addr=$(dig +short "$ns" A | head -1)
    answer=$(dig @"$addr" "$domain" NS +norec +noall +comments +answer)
    flags=$(echo "$answer" | grep -o 'flags: [a-z ]*' | head -1)
    child_ns=$(echo "$answer" | awk '$4 == "NS" {print tolower($5)}' | sort)
    case "$flags" in
        *aa*) ;;
        *) echo "LAME  $ns ($addr) does not answer authoritatively for $domain"; continue ;;
    esac
    if [ "$child_ns" = "$parent_ns" ]; then
        echo "OK    $ns ($addr) child NS set matches the parent"
    else
        echo "DIFF  $ns ($addr) child says: $(echo $child_ns)"
    fi
done
