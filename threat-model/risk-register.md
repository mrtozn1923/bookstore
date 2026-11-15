# Risk register (accepted risks)

An accepted risk is a decision, not an omission. Every row has an owner and a review date;
an expired row goes back to the backlog.

| Id | Threat | Decision | Why | Owner | Review |
|---|---|---|---|---|---|
| R-01 | T07 Customer data readable from backups | Accept residual risk | Backups are encrypted and the storage account allows only the backup identity; the rest needs a second key management system | Emre | 2027-05-01 |
| R-02 | T12 Splitting a request to stay under the approval limit | Closed: fixed after pentest finding F-01 (rolling 24 h unit total) | The pentest showed the daily report was not enough for institutional customers | Selin | Closed |
| R-03 | security.txt is not signed and has no Encryption field | Accept until the team runs OpenPGP key management | A key nobody manages is worse than none; the HTTPS report form is the second Contact | Emre | 2027-05-01 |
