/*
    Post 13 - pg_cron in a separate, throwaway container (not the bookstore-pg lab):
        docker build -t bookstore/postgres-cron:18 db/13-pg-cron
        docker run -d --name bookstore-cron -e POSTGRES_PASSWORD=<password> bookstore/postgres-cron:18 `
            -c shared_preload_libraries=pg_cron -c cron.database_name=bookstore -c cron.timezone=UTC
        docker exec -it bookstore-cron psql -U postgres -c "CREATE DATABASE bookstore"
        docker cp db/13-pg-cron/cron.sql bookstore-cron:/tmp/cron.sql
        docker exec -it bookstore-cron psql -U postgres -d bookstore -f /tmp/cron.sql
    Wait a minute, then: SELECT jobid, status, return_message FROM cron.job_run_details;
*/
CREATE EXTENSION pg_cron;
\dx pg_cron

CREATE TABLE outbox_messages (
    id int GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    payload text NOT NULL,
    processed_at timestamptz
);
INSERT INTO outbox_messages (payload, processed_at)
SELECT 'order ' || g, CASE WHEN g <= 900 THEN now() - interval '8 days' END
FROM generate_series(1, 1000) AS g;

/* every minute (cron syntax); in production something like '30 3 * * *' */
SELECT cron.schedule('purge-processed-outbox', '* * * * *',
    $$DELETE FROM outbox_messages WHERE processed_at < now() - interval '7 days'$$);
SELECT jobid, jobname, schedule, database, username, active FROM cron.job;
