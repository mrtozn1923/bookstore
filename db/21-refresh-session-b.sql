/*
    Post 21 - session B: read the materialized view while session A refreshes it.
*/
\pset footer off
SET lock_timeout = '2s';
SELECT l.mode, l.granted, a.query
FROM pg_locks AS l
JOIN pg_stat_activity AS a ON a.pid = l.pid
WHERE l.relation = 'mv_top_books'::regclass AND a.pid <> pg_backend_pid();
SELECT count(*) AS rows FROM mv_top_books;
