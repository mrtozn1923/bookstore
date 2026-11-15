#!/usr/bin/env bash
# Install script for the Bookstore Codex Cloud environment (Linux).
# Allowed domains it needs: dot.net, builds.dotnet.microsoft.com, api.nuget.org and the npm registry.
set -euo pipefail

# .NET 10 SDK, unless the image already has one.
if ! command -v dotnet >/dev/null 2>&1 || ! dotnet --list-sdks | grep -q '^10\.'; then
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --channel 10.0 --install-dir "$HOME/.dotnet"
  export DOTNET_ROOT="$HOME/.dotnet"
  export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"
  {
    echo 'export DOTNET_ROOT="$HOME/.dotnet"'
    echo 'export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"'
  } >> "$HOME/.bashrc"
fi
dotnet --version
node --version

# Dependencies and a first build, so tasks start with a warm cache.
dotnet restore Bookstore.slnx
dotnet build Bookstore.slnx --no-restore
(cd web/bookstore-web && npm ci)
