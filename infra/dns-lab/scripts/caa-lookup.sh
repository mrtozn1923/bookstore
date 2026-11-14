#!/bin/sh
# Usage: caa-lookup.sh <name> <server>
# Finds the CAA record set a CA would use: the name itself, then each parent.
name=${1%.}; server=$2
while [ -n "$name" ]; do
    caa=$(dig @"$server" "$name" CAA +noall +answer | awk '$4 == "CAA"')
    if [ -n "$caa" ]; then
        echo "relevant CAA set found at $name:"
        echo "$caa" | awk '{$1=$2=$3=$4=""; print " " $0}'
        exit 0
    fi
    echo "no CAA at $name, climbing"
    case "$name" in *.*) name=${name#*.} ;; *) name="" ;; esac
done
echo "no CAA anywhere: any CA may issue"
