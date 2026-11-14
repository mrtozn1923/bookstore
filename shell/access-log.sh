#!/usr/bin/env bash
# status, method, path and duration for every request; slowest first
sed -E 's/^[^"]*"([A-Z]+) ([^ "]+) [^"]*" ([0-9]{3}) .* ([0-9.]+)$/\4 \3 \1 \2/' data/access.log | sort -rn
