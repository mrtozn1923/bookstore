# Snapshot 32: git config

- `home/.gitconfig`: personal identity, `includeIf` for work repos (folder and remote URL), SSH alias rewrite with `url.insteadOf`, `useHttpPath` for github.com, and the team settings include.
- `home/.gitconfig-work`: work e-mail and SSH commit signing.
- `home/.ssh/config`: two GitHub accounts through the `github-work` host alias.
- `home/team.gitconfig`: recommended team settings.
- `fake-helper.sh`: test credential helper used in the post; it only prints what Git sends and returns fake values. Never use it for real credentials.

Copy the `home/` files to your own home folder and adjust names, e-mails and key paths.
