# KS-12 Purchase approval above the unit limit

As an institution approver, I want requests above my unit's limit to wait for my approval,
so that the school does not spend more than its budget allows without a second person.

## Acceptance criteria

- A request whose total is above the unit's approval limit is created as `PendingApproval`.
- An approver of the same institution can approve it; the request becomes `Approved`.
- The order can be placed only for an `Approved` request.

## Security acceptance criteria

- The requester cannot approve their own request, even with both roles. (ASVS v5.0.0-2.3.5)
- A buyer without the approver role gets 403 on approve. (ASVS v5.0.0-8.2.1)
- An approver of another institution gets 404, not 403. (ASVS v5.0.0-8.2.2)
- Calling "place order" on a `PendingApproval` request returns 409. (ASVS v5.0.0-2.3.1)
- Changing the lines after approval needs a new approval. (threat T10)

## Abuse story

As a buyer who wants a large order without waiting, I split it into several requests under the limit.
Decision: accepted for release 1, see `threat-model/risk-register.md` R-02.
