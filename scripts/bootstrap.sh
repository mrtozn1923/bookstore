#!/usr/bin/env bash
# Bootstrap a macOS, Linux or WSL machine for Bookstore. Safe to run more than once.
set -euo pipefail

cd "$(dirname "$0")/.."

case "$(uname -s)" in
  Darwin)
    if ! command -v brew >/dev/null 2>&1; then
      echo "Homebrew is missing. Install it from https://brew.sh and run this script again." >&2
      exit 1
    fi
    echo "==> Machine tools (Brewfile)"
    brew bundle --file=Brewfile --no-upgrade
    ;;
  Linux)
    echo "==> Machine tools (scripts/apt-packages.txt)"
    sudo apt-get update
    xargs -a scripts/apt-packages.txt sudo apt-get install -y
    for tool in mise chezmoi; do
      if ! command -v "$tool" >/dev/null 2>&1; then
        echo "$tool is missing. See the install steps in README.md." >&2
        exit 1
      fi
    done
    ;;
  *)
    echo "Unsupported OS: $(uname -s). On Windows use scripts/bootstrap.ps1." >&2
    exit 1
    ;;
esac

echo "==> Project runtimes (mise.toml, global.json)"
mise install

if [[ -n "${DOTFILES_REPO:-}" ]]; then
  echo "==> Dotfiles"
  if [[ -d "${XDG_DATA_HOME:-$HOME/.local/share}/chezmoi" ]]; then
    chezmoi update
  else
    chezmoi init --apply "$DOTFILES_REPO"
  fi
fi

echo "==> Project setup"
mise exec -- just setup

echo "Done. Start Bookstore with: mise exec -- just dev"
