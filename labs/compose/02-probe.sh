#!/usr/bin/env bash
# usage: labs/compose/02-probe.sh 50 "docker compose stop db; sleep 20; docker compose start db"
# requests /api/books every 0.5 s while an action runs; prints elapsed second, status code and request duration in ms
n=$1; action=$2; ms() { echo $(( ${EPOCHREALTIME/./} / 1000 )); }; start=$(ms)
(sleep 1; bash -c "$action" >/dev/null 2>&1; echo "action done at $(( ($(ms)-start)/1000 ))s" >&2) &
for i in $(seq 1 $n); do
  t0=$(ms); c=$(curl -s -o /dev/null -m 30 -w "%{http_code}" localhost:8080/api/books); t1=$(ms)
  echo "$(( (t0-start)/1000 ))s $c $(( t1-t0 ))ms"; sleep 0.5
done
