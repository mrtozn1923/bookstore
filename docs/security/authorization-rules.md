# Authorization rules and business limits (ASVS v5.0.0-8.1.1, v5.0.0-2.1.3)

## Institutional purchase requests

| Action | Who | Data rule | Business rule |
|---|---|---|---|
| Create a request | `InstitutionBuyer` | Own institution and unit (from token claims) | At least 20 copies; price = catalog price - 10% |
| Change lines | The requester | Own institution | Not after `Ordered`; an approved request goes back to `PendingApproval` |
| Approve | `InstitutionApprover` | Own institution; another institution's request is "not found" | Not the requester (four eyes); only `PendingApproval` |
| Place the order | `InstitutionBuyer` | Own institution | Only `Approved`, and the lines must match the approved lines |

## Limits

| Limit | Value | Scope |
|---|---|---|
| Approval limit | Per unit (`InstitutionUnits.ApprovalLimit`), Lakeside School Library: 5,000 TRY | Per request |
| Minimum copies | 20 | Per request |
| Daily total per unit without approval | Not enforced in release 1 (risk-register.md R-02) | Per unit, rolling 24 h |
