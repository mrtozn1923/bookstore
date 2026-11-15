---
name: Nightly issue digest
agent: ../agents/issue-digest.md
environment_id: ../environments/bookstore-cloud.yaml
schedule:
  type: cron
  expression: "0 6 * * 1-5"
  timezone: Europe/Istanbul
---

Write the issue digest for today.
