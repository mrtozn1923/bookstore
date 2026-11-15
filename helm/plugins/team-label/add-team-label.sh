#!/usr/bin/env sh
# Post-renderer: reads the rendered manifests on stdin, writes them to stdout with a team label.
sed 's/^  labels:$/  labels:\n    example.com\/team: bookstore/'
