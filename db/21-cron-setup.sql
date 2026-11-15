/*
    Post 21 - pg_cron (once, as postgres, in the pg_cron container from post 13):
        docker exec -it bookstore-cron psql -U postgres -d bookstore -f /lab/21-cron-setup.sql
*/
CREATE EXTENSION IF NOT EXISTS pg_cron;
GRANT USAGE ON SCHEMA cron TO bookstore_owner;
