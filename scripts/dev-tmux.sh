#!/usr/bin/env bash
# Opens (or re-attaches to) the Bookstore development session:
# window "dev": API (dotnet watch) | Angular (ng serve) / free shell
# window "git": a shell for git and tests
set -euo pipefail

session=bookstore
root=$(git -C "$(dirname "$0")" rev-parse --show-toplevel)

if ! tmux has-session -t "$session" 2>/dev/null; then
    api=$(tmux new-session -d -s "$session" -n dev -c "$root" -P -F '#{pane_id}')
    web=$(tmux split-window -h -t "$api" -c "$root/web/bookstore-web" -P -F '#{pane_id}')
    shell=$(tmux split-window -v -t "$api" -c "$root" -P -F '#{pane_id}')

    tmux send-keys -t "$api" 'dotnet watch --project src/Bookstore.Api' C-m
    tmux send-keys -t "$web" 'ng serve' C-m
    tmux send-keys -t "$shell" 'git status -sb' C-m

    tmux new-window -d -t "$session" -n git -c "$root"
    tmux select-pane -t "$shell"
fi

if [ -n "${TMUX:-}" ]; then
    tmux switch-client -t "$session"
else
    tmux attach -t "$session"
fi
