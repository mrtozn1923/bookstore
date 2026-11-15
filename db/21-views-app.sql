/*
    Post 21 - the same rows through a table and two views, as bookstore_app:
        docker exec -it bookstore-cron psql -U bookstore_app -d bookstore -f /lab/21-views-app.sql
*/
\pset footer off
SET app.customer_id = '3';
SELECT 'table' AS source, count(*) AS rows, count(DISTINCT customer_id) AS customers FROM customer_notes
UNION ALL
SELECT 'view (owner)', count(*), count(DISTINCT customer_id) FROM v_customer_notes
UNION ALL
SELECT 'view (security_invoker)', count(*), count(DISTINCT customer_id) FROM v_customer_notes_invoker;
