#!/usr/bin/env bash
# Ephemeral agent: registers itself, runs exactly one job (--once) and removes its registration on exit.
# The orchestrator starts a fresh container for the next job, so nothing survives from one job to the next.
set -euo pipefail

: "${AZP_URL:?AZP_URL is required, for example https://dev.azure.com/bookstore-org}"
: "${AZP_TOKEN_FILE:?AZP_TOKEN_FILE is required: a mounted file that holds the registration token}"
pool="${AZP_POOL:-bookstore-linux}"
agent_name="${AZP_AGENT_NAME:-bookstore-$(hostname)}"

# A shell variable, not exported: jobs started by run.sh never see the token.
token="$(cat "$AZP_TOKEN_FILE")"

package_url=$(curl -fsS -u "agent:$token" \
  "$AZP_URL/_apis/distributedtask/packages/agent?platform=$TARGETARCH&top=1" | jq -r '.value[0].downloadUrl')
curl -fsSL "$package_url" | tar -xz

cleanup() {
  VSTS_AGENT_INPUT_TOKEN="$token" ./config.sh remove --unattended --auth pat || true
}
trap cleanup EXIT

# config.sh reads --token from VSTS_AGENT_INPUT_TOKEN, so the token never appears on a command line.
VSTS_AGENT_INPUT_TOKEN="$token" ./config.sh --unattended --url "$AZP_URL" --auth pat \
  --pool "$pool" --agent "$agent_name" --replace --work _work

./run.sh --once
