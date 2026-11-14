#!/usr/bin/env bash
# "Herbert, Frank" -> "Frank Herbert" (last column only)
sed -E 's/"([^",]+), ([^"]+)"$/"\2 \1"/' data/authors.csv
echo
# GNU sed: \U upper-cases until \E
sed -E 's/"([^",]+), ([^"]+)"$/"\U\1\E, \2"/' data/authors.csv
