# Snapshot 33: searching history

- `.gitattributes`: `*.cs diff=csharp` so that `git grep -p` and `git log -L :Method:file` find C# methods.
- `.git-blame-ignore-revs`: the formatting commit skipped by `git blame` and GitHub's blame view. Run `git config blame.ignoreRevsFile .git-blame-ignore-revs` once per clone.
- `src/`: the discount code as it is at the end of the post (the `>` bug is still there; the post only finds it).
