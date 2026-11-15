#!/bin/sh
# Test credential helper: logs what Git sends, answers with obviously fake values.
[ "$1" = get ] || exit 0
input=$(cat)
printf -- '--- helper received:\n%s\n--- end\n' "$input" >&2
user=$(printf '%s\n' "$input" | sed -n 's/^username=//p')
echo "username=${user:-ada-personal}"
echo "password=fake-token-for-${user:-ada-personal}"
