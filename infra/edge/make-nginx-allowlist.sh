#!/bin/sh
# Turns cloudflare-ips.txt into nginx allow rules; everything else is denied.
# Usage: make-nginx-allowlist.sh cloudflare-ips.txt > cloudflare-allow.conf
echo "# generated from Cloudflare's published ranges, do not edit"
while read -r range; do
    [ -n "$range" ] && echo "allow $range;"
done < "$1"
echo "deny all;"
