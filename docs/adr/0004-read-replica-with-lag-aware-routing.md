# 4. Serve "My orders" and catalog reads from a read replica with lag-aware routing

Status: Accepted (2026-11)

## Context
Orders are expected to grow 10x in a year (1,290/day -> ~12,900/day). SLO: the "My orders" database
query stays under 50 ms at p99 in 5-minute windows, also at campaign peaks. Reads are ~95% of
database calls. The primary also serves checkout, payments and stock, and must keep headroom for them.

## Decision
- Add one async streaming replica; use it for reads that tolerate a few seconds of staleness.
- BookstoreReadContext (no tracking, no SaveChanges) on Npgsql "prefer-standby"; writes on "primary".
- ReadRouter sends a read to the primary when the replica is unreachable, lags more than 2 s, or has
  not replayed the user's last write LSN.
- Checkout, payment, stock and "decide-then-write" reads always stay on the primary.

## Consequences
- Read-your-writes holds for the writing user; other users may see data up to 2 s old.
- When the replica falls behind, all reads go to the primary: the primary is sized for the full read load.
- Two pools per API instance: the connection budget counts both against the primary.
- Long reports move to a separate reporting database (logical replication), not to this replica.

## Verification
- 5-minute load at campaign peak: read p99 < 50 ms, 0 errors (see post 10 measurements).
- Quarterly failover drill: write gap and lost acknowledged orders are recorded.
