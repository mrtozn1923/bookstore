#!/bin/sh
# Runs inside python:3.13-slim attached to the dnslab network (see compose.yaml).
set -e
pip install -q --root-user-action=ignore dkimpy==1.1.8 pyspf==2.0.14 dnspython authheaders==0.16.3
# lab-only: .example is not on the Public Suffix List, add it so alignment is computed correctly
d=$(python -c "import authheaders,os;print(os.path.dirname(authheaders.__file__))")
cp "$d/public_suffix_list.txt" /work/psl-lab.txt
printf '\nexample\n' >> /work/psl-lab.txt
echo 'location = "/work/psl-lab.txt"' > "$d/findpsl.py"
# resolve everything through the lab DNS server
echo "nameserver 192.0.2.20" > /etc/resolv.conf
[ -f /work/s1.private.pem ] || echo "generate keys first: openssl genrsa -out s1.private.pem 2048 (and tx1.private.pem)"
