# Incidents: Bookstore

How the Bookstore team declares and runs an incident. Runbooks (`docs/runbooks/`) cover the first five minutes of a
single alert; this page starts when one person is no longer enough.

## Declare early
Declare an incident (post in `#incident` and start a document from `TEMPLATE.md`) if any of these is true:
- Customers can see the problem (checkout, search or the institutional portal fails or is clearly slow).
- A second person or another team is needed to fix it.
- The on-call has not found a cause or a mitigation in 30 minutes.
- A runbook says so (e.g. checkout error ratio above 50 % for 15 minutes).

Declaring costs one message. Closing a small incident after ten minutes is fine; a late declaration is not.

## Severity
| Level | Meaning for Bookstore | Example | Response |
|---|---|---|---|
| SEV1 | Most customers cannot buy, or data/money is wrong | more than 50 % of checkouts fail for 5 minutes; orders charged twice | IC now, all hands, status page within 15 minutes, updates every 30 minutes |
| SEV2 | A critical journey is broken for a part of the customers | more than 10 % of checkouts fail for 15 minutes; institutional portal down | IC now, status page within 30 minutes, updates every 30 minutes |
| SEV3 | Small impact or a workaround exists | search slow for one category; back office report fails | on-call handles it in working hours or as the page says; no IC needed |
| SEV4 | No customer impact yet | a disk fills up slowly; a certificate expires in 10 days | ticket |

- Not sure which level? Take the higher one. The IC may lower it later.
- Do not argue about the level during the incident; the postmortem review can change it.
- SEV1 and SEV2 always get a postmortem (`docs/slo/error-budget-policy.md` adds budget-based ones).

## Roles
| Role | Does | Does not |
|---|---|---|
| Incident commander (IC) | decides, assigns work by name, sets the next update time, declares the end | debug or type commands |
| Ops lead | investigates and changes the system; the only one who changes production | talk to customers |
| Comms lead | status page, support team (Gul), management; drafts, IC approves | change production |
| Scribe | keeps the timeline in the incident document, with UTC times | decide |

In a three-person team: the on-call who got the page is ops lead; the next person to join becomes IC and also
writes the timeline; Selin (product owner) is comms lead. Handing over IC is said out loud and confirmed:
"I am handing over IC to Ayse." "I am IC."

## Mitigate first
Stop the customer impact first; find the root cause after. Prepared levers: `levers.md`.
