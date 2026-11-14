# Drill log

| Date (UTC) | Drill | Result | Numbers | Found / changed |
|---|---|---|---|---|
| 2026-11-14 22:53 | Restore (lab, BookstoreDb about 8 MB) | failed, then passed | backup 48 ms, restore 287 ms, CHECKDB 109 ms; row counts equal | First run failed: there was no full backup from a backup job at all, only the drill's own. Added the 26-hour check (step 8). |
