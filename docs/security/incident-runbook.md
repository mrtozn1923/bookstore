# Application security incident runbook

Use together with the general incident process (severity, incident commander, status updates).
This runbook adds what is specific to a security incident.

## First hour

1. Open a private incident channel; invite only who is needed. No details in public channels or tickets.
2. Name the roles: incident commander, technical lead, scribe, and a communications owner.
   Call the data protection contact (KVKK) and legal early: if personal data may be affected, the 72-hour
   clock for notifying the authority may already be running.
3. Preserve evidence before you change anything:
   `dotnet run --project tools/Bookstore.IncidentTools -- preserve <db> <evidence-dir>`,
   export the logs and traces of the time window, record who did what and when (UTC) in the incident log.
4. Contain without tipping off: prefer a quiet fix or a feature flag over deleting accounts or data.
   Do not rotate or delete anything that is evidence until it is preserved.

## Scoping

- Build the list of affected records from the audit log and traces, never from estimates:
  `dotnet run --project tools/Bookstore.IncidentTools -- affected <working-copy-db> <fromUtc> <toUtc>`.
- Work on a copy of the evidence, never on the evidence itself; check the manifest hashes before and after.
- Write down the window you could check and the window you could not (log retention, missing audit).

## Communication

- Affected customers: what happened, which of their data, what we did, what they should do, a contact person.
- Authority notification (KVKK): owned by the data protection contact, not by engineering.
- Status page: "we are investigating unauthorised access to institutional order lists" is enough; no exploit details.

## After the incident

- Security postmortem within 10 business days. Share internally: timeline, root cause, actions.
  Do not share outside the team: exploit steps for unfixed issues, names of individuals, raw evidence.
- Regression test with `[Trait("incident", "<id>")]`, threat model update, ASVS requirement if one was missing.

## Tabletop exercise (every 6 months, 60 minutes)

Scenario cards: a customer reports seeing another institution's data; a payment webhook secret leaks;
the audit log has a 2-day gap. For each card: who is called first, which command is run first, what is said to the customer.
