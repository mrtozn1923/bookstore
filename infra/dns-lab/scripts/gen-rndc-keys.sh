#!/bin/sh
# Creates the rndc keys the lab's named.conf files include. Run once before "docker compose up".
for dir in registry dnshost; do
    docker run --rm --entrypoint sh internetsystemsconsortium/bind9:9.20 \
        -c 'rndc-confgen -a -c /tmp/k -k rndc-key >/dev/null 2>&1; cat /tmp/k' > "$dir/rndc.key"
done
