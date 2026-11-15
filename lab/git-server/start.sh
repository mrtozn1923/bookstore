#!/bin/sh
set -e
spawn-fcgi -s /run/fcgiwrap.sock -u nginx -g nginx -- /usr/bin/fcgiwrap
exec nginx -g 'daemon off;'
