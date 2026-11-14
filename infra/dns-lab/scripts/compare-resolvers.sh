#!/bin/sh
# Usage: compare-resolvers.sh <name> <type>
# Asks the authoritative servers and a few public resolvers, prints what each one sees.
name=$1; type=$2
for ns in $(dig +short "$name" NS @1.1.1.1 | head -2) ; do
    printf '%-28s %s\n' "auth $ns" "$(dig +norec +short "$name" "$type" @"$ns" | tr '\n' ' ')"
done
for r in 1.1.1.1 8.8.8.8 9.9.9.9; do
    printf '%-28s %s\n' "resolver $r" "$(dig +noall +answer "$name" "$type" @"$r" | awk '{print "ttl=" $2, $5}' | tr '\n' ' ')"
done
