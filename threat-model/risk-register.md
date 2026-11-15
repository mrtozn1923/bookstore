# Risk register (accepted risks)

An accepted risk is a decision, not an omission. Every row has an owner and a review date;
an expired row goes back to the backlog.

| Id | Threat | Decision | Why | Owner | Review |
|---|---|---|---|---|---|
| R-01 | T07 Customer data readable from backups | Accept residual risk | Backups are encrypted and the storage account allows only the backup identity; the rest needs a second key management system | Emre | 2027-05-01 |
| R-02 | T12 Splitting a request to stay under the approval limit | Accept for release 1 | Each split request is still under the unit's limit and visible in the daily report; the rolling limit needs a product decision | Selin | 2027-03-01 |
