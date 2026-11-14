# Drills: Bookstore

Recovery paths that are not tried do not work when they are needed (GitLab 2017: none of the backup methods worked).

| Drill | How | How often | Owner |
|---|---|---|---|
| Restore | `deploy/drills/restore-drill.sql` against production's SQL Server (restores next to the original, then drops it) | monthly, and after every change to the backup job | Emre |
| Break-glass access | follow `docs/runbooks/break-glass.md` from a laptop outside the office network | every quarter | Selin |
| Mixed versions after deploy | `MixedVersionsAfterDeploy` alert (`deploy/prometheus/rules/deploy-alerts.yaml`); after each deploy check `count by (service_version) (target_info{job="bookstore-api"})` | every deploy | whoever deploys |

Write every run into `log.md`: date, result, durations, what was wrong.
