SELECT pg_typeof(now()) AS now_type, pg_typeof(localtimestamp) AS localts_type, pg_typeof(current_date) AS date_type;
SELECT date '2026-01-31' + interval '1 month' AS jan31_plus_month,
       date_trunc('month', timestamp '2026-06-20 14:05') AS trunc_month,
       (date_trunc('month', date '2026-02-10') + interval '1 month - 1 day')::date AS end_of_month,
       make_date(2026, 6, 1) AS from_parts;
SELECT age(date '2026-06-20', date '2000-12-31') AS age_interval,
       extract(year FROM age(date '2026-06-20', date '2000-12-31')) AS age_years,
       date '2026-06-21' - date '2026-06-20' AS day_diff,
       extract(epoch FROM timestamp '2026-01-01' - timestamp '2025-01-01') * 1000 AS ms_in_2025;
SELECT timestamptz '2025-12-31 23:06:27+00' AT TIME ZONE 'Europe/Istanbul' AS istanbul_local,
       timestamp '2025-12-31 23:06:27' AT TIME ZONE 'UTC' AS ts_to_tz;
SELECT extract(week FROM date '2025-12-29') AS iso_week, extract(isodow FROM date '2026-06-20') AS isodow, to_char(date '2026-06-20', 'TMDay') AS day_name;
SELECT make_date(2026, 2, 30);
