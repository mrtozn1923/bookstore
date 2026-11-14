#!/usr/bin/env bash
set -euo pipefail
# work on a throwaway git copy of sample/ so every change can be reviewed and undone
repo=$(mktemp -d)
cp -r sample/. "$repo"
cd "$repo"
printf 'obj/\n' > .gitignore
git init -q && git add . && git -c user.name=Ada -c user.email=ada@example.com commit -qm "before refactor"
# build output that is not tracked by git
mkdir -p Bookstore.Catalog/obj
echo 'logger.LogDebug($"Generated {id}");' > Bookstore.Catalog/obj/Generated.cs

# one placeholder, a plain identifier: Log...($"text {name} text")
find='(Log[A-Za-z]+)\(\$"([^"{]*)\{([A-Za-z_][A-Za-z0-9_]*)\}([^"{]*)"\)'

echo "== which files?"
echo "grep -r: $(grep -rlE "$find" . | wc -l) files, git grep: $(git grep -lE "$find" | wc -l) files"

echo "== preview (rg -r changes nothing on disk)"
rg --sort path -n -o "$find" -r '$1("$2{$3}$4", $3)' .

echo "== apply (GNU sed, \u upper-cases the next character)"
git grep -lE "$find" | xargs sed -E -i "s/$find/\1(\"\2{\u\3}\4\", \3)/"
git diff --stat
git diff -U0 | grep '^[-+] '

echo "== still interpolated"
git grep -nE 'Log[A-Za-z]+\(\s*(\$"|$)'
rm -rf "$repo"
