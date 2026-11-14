#!/usr/bin/env bash
# Usage: ./check-domain-expiry.sh 30 example.com example.net
set -euo pipefail
warn_days=$1; shift
bootstrap=$(curl -s https://data.iana.org/rdap/dns.json)
now=$(date -u +%s)
status=0
for domain in "$@"; do
    tld=${domain##*.}
    base=$(jq -r --arg t "$tld" '.services[] | select(.[0] | index($t)) | .[1][0]' <<<"$bootstrap")
    if [[ -z "$base" ]]; then
        echo "$domain: no RDAP service for .$tld, check the registrar panel"
        status=2; continue
    fi
    expires=$(curl -s "${base%/}/domain/$domain" | jq -r '.events[] | select(.eventAction == "expiration") | .eventDate')
    days=$(( ($(date -u -j -f "%Y-%m-%dT%H:%M:%SZ" "$expires" +%s 2>/dev/null || date -u -d "$expires" +%s) - now) / 86400 ))
    if (( days < warn_days )); then
        echo "WARN $domain expires in $days days ($expires)"; status=1
    else
        echo "OK   $domain expires in $days days ($expires)"
    fi
done
exit $status
