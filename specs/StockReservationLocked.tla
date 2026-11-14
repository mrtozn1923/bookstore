---------------------- MODULE StockReservationLocked ----------------------
EXTENDS Integers

CONSTANTS Buyers, InitialStock

VARIABLES stock, sold, pc

vars == <<stock, sold, pc>>

Init ==
    /\ stock = InitialStock
    /\ sold = 0
    /\ pc = [b \in Buyers |-> "reserve"]

\* lock { if (stock > 0) stock--; }
CheckAndReserve(b) ==
    /\ pc[b] = "reserve"
    /\ IF stock > 0
          THEN /\ stock' = stock - 1
               /\ sold' = sold + 1
          ELSE UNCHANGED <<stock, sold>>
    /\ pc' = [pc EXCEPT ![b] = "done"]

Finished ==
    /\ \A b \in Buyers : pc[b] = "done"
    /\ UNCHANGED vars

Next == (\E b \in Buyers : CheckAndReserve(b)) \/ Finished

Spec == Init /\ [][Next]_vars

NoOversell == sold <= InitialStock
=============================================================================
