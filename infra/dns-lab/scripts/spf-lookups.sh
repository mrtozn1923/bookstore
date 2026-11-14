#!/bin/bash
# Usage: spf-lookups.sh <domain> [resolver]
# Prints the SPF include tree and counts DNS-querying terms (limit: 10).
resolver=${2:+@$2}
total=0
walk() {
    local domain=$1 indent=$2 record term
    record=$(dig $resolver +short "$domain" TXT | tr -d '"' | grep '^v=spf1')
    if [[ $(grep -c . <<<"$record") -gt 1 ]]; then
        echo "${indent}$domain: MORE THAN ONE SPF RECORD (permerror)"; return
    fi
    echo "${indent}$domain"
    for term in $record; do
        case "$term" in
            include:*|redirect=*)
                total=$((total + 1)); walk "${term#*[:=]}" "$indent  " ;;
            a|a:*|mx|mx:*|ptr|ptr:*|exists:*)
                total=$((total + 1)); echo "$indent  $term" ;;
        esac
    done
}
walk "$1" ""
echo "DNS lookups: $total/10"
(( total <= 10 ))
