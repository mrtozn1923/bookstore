#!/bin/sh
# Keys come from a read-only mount (/keys); sshd wants them owned by root with strict permissions.
set -e
install -m 600 /keys/ssh_host_ed25519_key /etc/ssh/ssh_host_ed25519_key
install -m 600 /keys/ssh_host_rsa_key /etc/ssh/ssh_host_rsa_key
mkdir -p /etc/ssh/authorized_keys
install -m 644 /keys/bookstore_supplier.pub /etc/ssh/authorized_keys/supplier
exec /usr/sbin/sshd -D -e
