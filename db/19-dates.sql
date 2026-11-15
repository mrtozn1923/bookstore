/*
    Post 19 - dates, times and intervals (PostgreSQL 18).
    Run as bookstore_owner in the bookstore database, right after a fresh schema.sql:
        docker exec -it bookstore-pg psql -U bookstore_owner -d bookstore -f /lab/19-dates.sql
    The session TimeZone of the lab is Etc/UTC. Leaves ix_orders_created_at behind (post 07 creates the same index).
*/
\pset footer off

/* guess first: three clocks inside one transaction */
BEGIN;
SELECT now(), statement_timestamp(), clock_timestamp();
SELECT pg_sleep(1.5);
SELECT now(), statement_timestamp(), clock_timestamp();
COMMIT;
SELECT now() = transaction_timestamp() AS same, current_timestamp, localtimestamp, current_date,
       pg_typeof(localtimestamp) AS localtimestamp_type;

/* time zones: the session setting and both directions of AT TIME ZONE */
SHOW TimeZone;
SELECT ts AS utc_instant,
       ts AT TIME ZONE 'Europe/Istanbul' AS istanbul_wall_clock,
       timestamp '2026-10-01 00:00' AT TIME ZONE 'Europe/Istanbul' AS istanbul_midnight,
       date_trunc('day', ts) AS trunc_session_tz,
       date_trunc('day', ts, 'Europe/Istanbul') AS trunc_istanbul
FROM (VALUES (timestamptz '2026-09-30 22:30:00+00')) AS v(ts);
SELECT (created_at AT TIME ZONE 'Europe/Istanbul')::date = date '2026-10-01' AS istanbul_oct1,
       created_at::date = date '2026-10-01' AS utc_oct1,
       count(*), min(order_number), max(order_number)
FROM orders
WHERE created_at >= '2026-09-30 00:00+00' AND created_at < '2026-10-02 00:00+00'
GROUP BY 1, 2
ORDER BY 1, 2;

/* interval arithmetic */
SELECT date '2026-01-31' + interval '1 month' AS jan31_plus_1m,
       date '2026-01-31' + interval '1 month' + interval '1 month' AS plus_twice,
       date '2026-01-31' + interval '2 months' AS plus_2m,
       date '2026-03-01' - date '2026-02-01' AS days_between,
       pg_typeof(date '2026-03-01' - date '2026-02-01') AS diff_type;
SELECT timestamptz '2026-10-20 21:18:00+00' - timestamptz '2023-01-01 00:00:00+00' AS diff,
       age(timestamptz '2026-10-20 21:18:00+00', timestamptz '2023-01-01 00:00:00+00') AS age_result,
       justify_interval(interval '50 days 30 hours') AS justified,
       extract(epoch FROM timestamptz '2026-10-20 21:18:00+00' - timestamptz '2023-01-01 00:00:00+00') / 86400 AS days;
SET TimeZone = 'Europe/Berlin';
SELECT ts + interval '1 day' AS plus_1_day, ts + interval '24 hours' AS plus_24_hours
FROM (VALUES (timestamptz '2026-03-28 12:00 Europe/Berlin')) AS v(ts);
RESET TimeZone;
SELECT date_add(timestamptz '2026-03-28 11:00+00', interval '1 day', 'Europe/Berlin') AS date_add_berlin,
       timestamptz '2026-03-28 11:00+00' + interval '1 day' AS plus_in_utc;

/* extract, make_*, to_char */
SELECT extract(year FROM ts) AS y, extract(isodow FROM ts) AS isodow, extract(dow FROM ts) AS dow,
       extract(week FROM ts) AS iso_week, extract(epoch FROM ts) AS epoch, date_part('hour', ts) AS h,
       pg_typeof(extract(epoch FROM ts)) AS extract_type, pg_typeof(date_part('hour', ts)) AS date_part_type
FROM (VALUES (timestamptz '2026-11-15 09:45:30+00')) AS v(ts);
SELECT make_date(2026, 2, 29);
SELECT make_date(2026, 12, 31) AS d, make_timestamptz(2026, 11, 15, 9, 30, 0, 'Europe/Istanbul') AS tstz,
       make_interval(days => 10, hours => 3) AS iv,
       date_trunc('month', date '2026-02-14') + interval '1 month - 1 day' AS end_of_month,
       (date_trunc('month', date '2026-02-14') + interval '1 month - 1 day')::date AS end_of_month_date;
SELECT to_char(ts AT TIME ZONE 'Europe/Istanbul', 'DD.MM.YYYY HH24:MI') AS tr_format,
       to_char(ts, 'YYYY-MM-DD"T"HH24:MI:SSOF') AS iso_like,
       to_char(ts, 'TMDay, DD TMMonth YYYY') AS long_en,
       to_char(ts, 'IYYY-"W"IW') AS iso_week
FROM (VALUES (timestamptz '2026-11-15 09:45:30+00')) AS v(ts);

/* time bucketing: date_trunc, date_bin and generate_series */
SELECT date_bin(interval '15 minutes', created_at, timestamptz '2026-01-01') AS bucket, count(*)
FROM orders
WHERE created_at >= '2026-10-20 20:00+00'
GROUP BY 1
ORDER BY 1;
SELECT d AS berlin_midnight
FROM generate_series(timestamptz '2026-03-28 00:00 Europe/Berlin', timestamptz '2026-03-31 00:00 Europe/Berlin',
                     interval '1 day', 'Europe/Berlin') AS d;

/* Bookstore: daily TRY sales by Istanbul day, empty days filled with zero */
WITH days AS (
    SELECT d::date AS day
    FROM generate_series(date '2026-10-17', date '2026-10-23', interval '1 day') AS d
),
sales AS (
    SELECT (created_at AT TIME ZONE 'Europe/Istanbul')::date AS day, count(*) AS orders, sum(total_amount) AS revenue
    FROM orders
    WHERE created_at >= timestamptz '2026-10-17 00:00 Europe/Istanbul'
      AND created_at <  timestamptz '2026-10-24 00:00 Europe/Istanbul'
      AND currency = 'TRY' AND status <> 'Cancelled'
    GROUP BY 1
)
SELECT days.day, coalesce(sales.orders, 0) AS orders, coalesce(sales.revenue, 0) AS revenue
FROM days
LEFT JOIN sales USING (day)
ORDER BY days.day;

/* indexes: a function on the column vs a range on the column */
CREATE INDEX ix_orders_created_at ON orders (created_at);
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF)
SELECT count(*) FROM orders WHERE date_trunc('day', created_at) = timestamptz '2026-10-01';
EXPLAIN (ANALYZE, COSTS OFF, TIMING OFF, SUMMARY OFF)
SELECT count(*) FROM orders WHERE created_at >= timestamptz '2026-10-01' AND created_at < timestamptz '2026-10-02';
CREATE INDEX ix_orders_created_day ON orders (date_trunc('day', created_at));
CREATE INDEX ix_orders_created_day_ist ON orders (date_trunc('day', created_at, 'Europe/Istanbul'));
SELECT proname, provolatile, pg_get_function_identity_arguments(oid) AS args
FROM pg_proc
WHERE proname = 'date_trunc'
ORDER BY 3;
DROP INDEX ix_orders_created_day_ist;

/* pitfalls: timestamp drops the offset, DEFAULT 'now' is frozen at CREATE TABLE */
CREATE TEMP TABLE import_log
(
    id          int,
    imported_at timestamp,
    created_at  timestamptz DEFAULT 'now',
    logged_at   timestamptz DEFAULT now()
);
INSERT INTO import_log (id, imported_at) VALUES (1, '2026-11-15 10:00:00+03');
SELECT pg_sleep(1);
INSERT INTO import_log (id, imported_at) VALUES (2, '2026-11-15 10:00:00+03');
SELECT id, imported_at, created_at, logged_at FROM import_log;
SELECT pg_get_expr(adbin, adrelid) AS default_expr FROM pg_attrdef WHERE adrelid = 'import_log'::regclass;
