------------------------- MODULE StockReservation -------------------------
EXTENDS Integers

CONSTANTS Buyers, InitialStock

VARIABLES stock, sold, pc

vars == <<stock, sold, pc>>

Init ==
    /\ stock = InitialStock
    /\ sold = 0
    /\ pc = [b \in Buyers |-> "check"]

\* if (stock > 0) ...
Check(b) ==
    /\ pc[b] = "check"
    /\ pc' = [pc EXCEPT ![b] = IF stock > 0 THEN "reserve" ELSE "done"]
    /\ UNCHANGED <<stock, sold>>

\* ... stock--;
Reserve(b) ==
    /\ pc[b] = "reserve"
    /\ stock' = stock - 1
    /\ sold' = sold + 1
    /\ pc' = [pc EXCEPT ![b] = "done"]

Finished ==
    /\ \A b \in Buyers : pc[b] = "done"
    /\ UNCHANGED vars

Next == (\E b \in Buyers : Check(b) \/ Reserve(b)) \/ Finished

Spec == Init /\ [][Next]_vars

NoOversell == sold <= InitialStock
=============================================================================
