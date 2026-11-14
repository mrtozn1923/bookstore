# Institutional Ordering Module - Business Requirements (v1.0)

- R1. An institution user creates an order: book, quantity, budget category, requested delivery date, optional note. Draft first, then submit.
- R2. On submission the system calculates the total: list price x quantity, minus the institution's discount (default 10 %).
- R3. Except for Donation, an order cannot be submitted if the unit's remaining budget for the year is less than the total.
- R4. A user cannot have two active orders for the same book.
- R6. A submitted order goes to the user's approver, who approves or rejects (a reason is mandatory when rejecting).
- R7. Orders above 50,000 TRY also need the approval of the purchasing manager of the user's unit.
- R8. The budget is checked again at approval time.
- R9. Final approval deducts the total from the unit's budget and sends the order to the book distributor.
- R11. Pending orders can be cancelled any time; approved ones only before the delivery date (the portal admin can cancel later).
- R13. An order may accept partial shipment (default: no): if the distributor is short of stock, it ships what it has first and the rest by the delivery date. The user may set a minimum quantity for the first shipment (at most the order quantity).
