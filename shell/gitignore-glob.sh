#!/usr/bin/env bash
# .gitignore lines are globs: a regex there silently matches nothing
repo=$(mktemp -d)
cd "$repo" && git init -q
mkdir -p src/Bookstore.Api/bin logs
touch src/Bookstore.Api/Program.cs src/Bookstore.Api/bin/Bookstore.Api.dll logs/app.log logs/app.log.1
printf '%s\n' '.*\.log$' 'bin' > .gitignore
git check-ignore -v --no-index src/Bookstore.Api/bin/Bookstore.Api.dll logs/app.log logs/app.log.1
echo '---'
printf '%s\n' '*.log' '*.log.[0-9]' 'bin/' > .gitignore
git check-ignore -v --no-index src/Bookstore.Api/bin/Bookstore.Api.dll logs/app.log logs/app.log.1
rm -rf "$repo"
