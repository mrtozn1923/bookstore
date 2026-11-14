# Postmortems: Bookstore

One file per incident: `yyyy-mm-dd-<topic>.md`, blameless (template and root cause techniques: Debugging and
Problem Solving series). This page is about the process around the document.

## When a postmortem is required
- SEV1 or SEV2.
- More than 20 % of a 30-day error budget spent by one incident (`docs/slo/error-budget-policy.md`).
- Any data loss, any wrong charge, or a security-relevant finding.
- A customer or support found it before an alert did.
- Production was changed by hand (rollback, lever in `docs/incidents/levers.md`, manual data fix).
- Anyone asks for one, including for a near miss.

## Flow and deadlines
| Step | Who | When |
|---|---|---|
| Draft (timeline from the incident document, impact in numbers, contributing factors, action items) | IC picks an author, usually the ops lead | within 3 working days |
| Review meeting (45 minutes) | author, IC, people who were in the incident, owner of each affected service; facilitator is not the author | within 5 working days |
| Publish (merge to `main`, post the link in #engineering) | author | after the review |
| Action items tracked | each owner; `postmortem-lint` in CI lists overdue ones | until done |

## Review meeting agenda
1. (5 min) The facilitator reads the impact and the timeline summary. No new debugging in the meeting.
2. (15 min) Timeline questions: when did we know, what did we believe, why did that make sense at the time?
3. (15 min) Contributing factors and action items: does each item make this incident less likely, find it sooner
   or make it smaller? One owner, a date, a ticket.
4. (5 min) What went well and should be kept.
5. (5 min) Decide: severity confirmed, postmortem approved or a second round.

## Action items
`| ID | Action | Class | Owner | Due | Ticket | Status |`, class is one of:
- prevent: the same trigger can no longer cause it,
- detect: we notice sooner (alert, probe, dashboard),
- mitigate: when it happens again, the impact is smaller or shorter (lever, runbook step, fallback),
- process: on-call, communication, review.

`dotnet run tools/postmortem-lint/postmortem-lint.cs` fails on an item without an owner, a date or a ticket and lists
overdue items.
