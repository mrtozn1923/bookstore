# Review: purchase approval API (2026-11-15)

Draft: draft.yaml. Reviewers: API owners (Ayşe, Emre); consumer: Lakeside School integration.
Result: changes requested; the revised design is in contract/bookstore.openapi.yaml (ADR-0008).

Found by the linter (4 errors): verb and camelCase in /getApprovals and /approvePurchase,
three path parameters in /institutions/{institutionId}/units/{unitId}/orders/{orderId}/approval.

Found in review:
1. approverName in the request body: the approver is the caller (token), never a field.
2. approved: boolean hides two different operations; reject needs a reason. Use :approve and :reject.
3. 200 with success false for every failure: use Problem Details (403, 404, 409, 412, 428).
4. Two approvers can decide at the same time: the decision is a conditional write (If-Match).
5. getApprovals returns every approval as an array: paginate (BS-30). The linter missed it
   because the operationId does not start with list.
6. Status values WAITING/OK/NOT_OK: use Pending/Approved/Rejected, documented as open (BS-52).
7. The approval has its own id and lifecycle: /purchase-approvals/{approvalId} is canonical,
   /institutions/{institutionId}/purchase-approvals is a filtered view (BS-06).
8. Partners want to know when a decision is made: describe a webhook (planned).

Open decisions (not blocking): an amount above which two approvers are needed (Release 2);
whether an approval expires; whether buyers may withdraw a pending request.
