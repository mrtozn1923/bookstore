#!/bin/sh
# After "docker compose up -d": publishes Bookstore's DS in the registry zone and
# makes the registry (the lab's validating resolver) trust the .example key.
set -e
ds=$(docker exec dnsseries-registry sh -c \
    'dig @192.0.2.20 bookstore.example DNSKEY +norec +noall +answer | dnssec-dsfromkey -2 -f - bookstore.example' | sed 's/.* DS //')
key=$(docker exec dnsseries-registry dig @192.0.2.10 example DNSKEY +norec +noall +answer +nosplit | awk '$5 == 257 {print $8}')
sed -i.bak '/^bookstore       IN DS/d' registry/example.zone
awk -v ds="$ds" '{print} /^bookstore       IN NS/ {print "bookstore       IN DS   " ds}' registry/example.zone > registry/example.zone.new
mv registry/example.zone.new registry/example.zone
serial=$(date -u +%Y%m%d%H)
sed -i.bak "s|^\( *\)[0-9]\{10\} ; serial|\1$serial ; serial|" registry/example.zone
printf 'trust-anchors {\n    example. static-key 257 3 13 "%s";\n};\n' "$key" > registry/trust-anchor.conf
sed -i.bak 's|dnssec-validation no;|dnssec-validation yes;|' registry/named.conf
grep -q 'trust-anchor.conf' registry/named.conf || sed -i.bak '1i\
include "/etc/bind/trust-anchor.conf";
' registry/named.conf
rm -f registry/*.bak
docker restart dnsseries-registry >/dev/null
sleep 5
docker exec dnsseries-registry dig @192.0.2.10 bookstore.example A +dnssec +noall +comments | grep flags
