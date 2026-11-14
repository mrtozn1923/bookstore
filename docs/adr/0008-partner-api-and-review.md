# ADR-0008: Purchase approvals in the partner API, and how APIs are reviewed

Status: Accepted (2026-11-15)

## Context
Institutional orders above the buyer's authority must be approved by the institution's
approver before Bookstore ships them. Lakeside School's integration team proposed a first
design (docs/reviews/2026-11-purchase-approvals/draft.yaml). It passed their own tests but
broke most of our style guide.

## Decision
- Every Pending institutional order gets a purchase approval: GET /purchase-approvals/{approvalId}
  is canonical, GET /institutions/{institutionId}/purchase-approvals is a paginated filtered view.
- Decisions are custom methods, :approve and :reject (with a reason), made only by an
  InstitutionApprover, and require If-Match. Deciding twice is 409 approval-already-decided.
- A purchaseApprovalDecided webhook is described in the contract and planned.
- API changes are reviewed on the contract pull request, before implementation, with
  docs/api-review-checklist.md. Tools check what they can; reviewers check the rest. A review
  that finds a rule worth automating adds a lint rule. Review records live in docs/reviews/.
- docs/api-catalog.md lists every API surface with its audience, owner and lifecycle.

## Consequences
- Partners can integrate against the Prism mock of the reviewed contract before the code ships.
- Exceptions to the style guide need an ADR; the review does not grant them silently.
