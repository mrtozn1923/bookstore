/*
    Post 21 - schedule the concurrent refresh every 10 minutes, as bookstore_owner (owner of mv_top_books).
    The pg_cron container of post 13, this time with the lab roles, the schema and /lab mounted (PowerShell):
        docker build -t bookstore/postgres-cron:18 db/13-pg-cron
        docker run -d --name bookstore-cron -e POSTGRES_PASSWORD=<password> -v "${PWD}\db:/lab:ro" bookstore/postgres-cron:18 `
            -c shared_preload_libraries=pg_cron -c cron.database_name=bookstore -c cron.timezone=UTC
        docker exec -it bookstore-cron psql -U postgres -f /lab/00-roles.sql
        docker exec -it bookstore-cron psql -U bookstore_owner -d bookstore -f /lab/schema.sql
        docker exec -it bookstore-cron psql -U bookstore_owner -d bookstore -f /lab/21-views.sql
        docker exec -it bookstore-cron psql -U bookstore_owner -d bookstore -f /lab/21-matview.sql
        docker exec -it bookstore-cron psql -U postgres -d bookstore -f /lab/21-cron-setup.sql
        docker exec -it bookstore-cron psql -U bookstore_owner -d bookstore -f /lab/21-cron.sql
    After the next 10-minute mark, run the last query of this file again.
*/
\pset footer off
SELECT cron.schedule('refresh-top-books', '*/10 * * * *',
    $$REFRESH MATERIALIZED VIEW CONCURRENTLY mv_top_books$$) AS jobid;
SELECT jobid, jobname, schedule, command, username, active FROM cron.job;
SELECT jobid, status, return_message, end_time - start_time AS duration
FROM cron.job_run_details
ORDER BY runid DESC
LIMIT 3;
