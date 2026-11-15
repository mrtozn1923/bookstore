/*
    Post 21 - session A: a refresh whose transaction stays open for 4 seconds.
    While it sleeps, run 21-refresh-session-b.sql in a second terminal.
        psql ... -f /lab/21-refresh-session-a.sql                       (plain REFRESH)
        psql ... -v concurrently=1 -f /lab/21-refresh-session-a.sql     (REFRESH ... CONCURRENTLY)
*/
\pset footer off
BEGIN;
\if :{?concurrently}
REFRESH MATERIALIZED VIEW CONCURRENTLY mv_top_books;
\else
REFRESH MATERIALIZED VIEW mv_top_books;
\endif
SELECT pg_sleep(4);
COMMIT;
