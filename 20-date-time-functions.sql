SET LANGUAGE Turkish;
SELECT CAST('2026-04-03' AS DATE) AS as_date,
       CAST('2026-04-03' AS DATETIME) AS as_datetime,
       CAST('20260403' AS DATETIME) AS unseparated,
       CAST('2026-04-03T00:00:00' AS DATETIME) AS iso_with_t;
SET LANGUAGE us_english;
GO
SELECT 'GETDATE()' AS fn, CAST(SQL_VARIANT_PROPERTY(GETDATE(), 'BaseType') AS VARCHAR(20)) AS base_type, CONVERT(VARCHAR(40), GETDATE(), 121) AS value
UNION ALL SELECT 'CURRENT_TIMESTAMP', CAST(SQL_VARIANT_PROPERTY(CURRENT_TIMESTAMP, 'BaseType') AS VARCHAR(20)), CONVERT(VARCHAR(40), CURRENT_TIMESTAMP, 121)
UNION ALL SELECT 'SYSDATETIME()', CAST(SQL_VARIANT_PROPERTY(SYSDATETIME(), 'BaseType') AS VARCHAR(20)), CONVERT(VARCHAR(40), SYSDATETIME(), 121)
UNION ALL SELECT 'SYSUTCDATETIME()', CAST(SQL_VARIANT_PROPERTY(SYSUTCDATETIME(), 'BaseType') AS VARCHAR(20)), CONVERT(VARCHAR(40), SYSUTCDATETIME(), 121)
UNION ALL SELECT 'SYSDATETIMEOFFSET()', CAST(SQL_VARIANT_PROPERTY(SYSDATETIMEOFFSET(), 'BaseType') AS VARCHAR(20)), CONVERT(VARCHAR(40), SYSDATETIMEOFFSET(), 121)
UNION ALL SELECT 'CURRENT_DATE', CAST(SQL_VARIANT_PROPERTY(CURRENT_DATE, 'BaseType') AS VARCHAR(20)), CONVERT(VARCHAR(40), CURRENT_DATE, 121);
GO
SELECT EOMONTH('2026-02-10') AS end_feb, EOMONTH('2026-02-10', 1) AS end_mar, EOMONTH('2028-02-10') AS leap_feb,
       DATEADD(DAY, 1, EOMONTH('2026-06-20', -1)) AS first_of_month,
       DATEFROMPARTS(2026, 6, 1) AS from_parts,
       DATETIME2FROMPARTS(2026, 6, 1, 9, 30, 0, 0, 0) AS dt2_from_parts,
       DATETRUNC(MONTH, CAST('2026-06-20 14:05' AS DATETIME2(0))) AS trunc_month;
GO
SELECT DATEFROMPARTS(2026, 2, 30) AS bad_date;
GO
DECLARE @birth DATE = '2000-12-31', @today DATE = '2026-06-20';
SELECT DATEDIFF(YEAR, @birth, @today) AS datediff_year,
       DATEDIFF(YEAR, @birth, @today)
         - CASE WHEN DATEADD(YEAR, DATEDIFF(YEAR, @birth, @today), @birth) > @today THEN 1 ELSE 0 END AS age;
GO
SET DATEFIRST 1;
SELECT DATEPART(WEEKDAY, '2026-06-20') AS weekday_monday_first,
       DATEDIFF(WEEK, '2026-06-20', '2026-06-21') AS week_diff_sat_sun,
       DATEDIFF(WEEK, '2026-06-21', '2026-06-22') AS week_diff_sun_mon;
SET DATEFIRST 7;
GO
SELECT DATEDIFF(MILLISECOND, '2025-01-01', '2026-01-01') AS ms_in_2025;
GO
SELECT DATEDIFF_BIG(MILLISECOND, '2025-01-01', '2026-01-01') AS ms_in_2025,
       DATEDIFF_BIG(MILLISECOND, '1970-01-01', '2026-01-01') AS unix_ms;
GO
SET LANGUAGE Turkish;
SELECT DATENAME(MONTH, '2026-06-20') AS month_name, DATENAME(WEEKDAY, '2026-06-20') AS day_name,
       DATEPART(WEEKDAY, '2026-06-20') AS weekday_no, @@DATEFIRST AS datefirst;
SET LANGUAGE us_english;
SELECT DATENAME(MONTH, '2026-06-20') AS month_name, DATENAME(WEEKDAY, '2026-06-20') AS day_name,
       DATEPART(WEEKDAY, '2026-06-20') AS weekday_no, @@DATEFIRST AS datefirst;
GO
SELECT d AS dt, DATEPART(WEEK, d) AS week_no, DATEPART(ISO_WEEK, d) AS iso_week, DATENAME(WEEKDAY, d) AS day_name
FROM (VALUES (CAST('2025-12-28' AS DATE)), ('2025-12-29'), ('2025-12-31'), ('2026-01-01'), ('2026-01-04'), ('2026-01-05')) AS v(d);
GO
SELECT
    (SELECT COUNT(*) FROM dbo.orders WHERE ordered_at BETWEEN '2025-03-01' AND '2025-03-31') AS between_dates,
    (SELECT COUNT(*) FROM dbo.orders WHERE ordered_at BETWEEN '2025-03-01' AND '2025-03-31 23:59:59.997') AS between_997,
    (SELECT COUNT(*) FROM dbo.orders WHERE ordered_at >= '2025-03-01' AND ordered_at < '2025-04-01') AS half_open;
GO
DECLARE @t TABLE (event_at DATETIME);
INSERT INTO @t VALUES ('2025-03-31 23:59:59.997'), ('2025-04-01 00:00:00');
SELECT COUNT(*) AS march_999 FROM @t WHERE event_at BETWEEN '2025-03-01' AND '2025-03-31 23:59:59.999';
GO
DECLARE @t2 TABLE (event_at DATETIME2(7));
INSERT INTO @t2 VALUES ('2025-03-31 23:59:59.9985'), ('2025-04-01 00:00:00');
SELECT COUNT(*) AS march_997_dt2 FROM @t2 WHERE event_at BETWEEN '2025-03-01' AND '2025-03-31 23:59:59.997';
GO
SELECT name, current_utc_offset, is_currently_dst
FROM sys.time_zone_info
WHERE name IN ('UTC', 'Turkey Standard Time', 'W. Europe Standard Time', 'GMT Standard Time')
ORDER BY name;
GO
SELECT order_id, ordered_at,
       ordered_at AT TIME ZONE 'UTC' AS utc_offset,
       ordered_at AT TIME ZONE 'UTC' AT TIME ZONE 'Turkey Standard Time' AS istanbul,
       ordered_at AT TIME ZONE 'Turkey Standard Time' AS wrong_way
FROM dbo.orders
WHERE order_id = 263;
GO
SELECT CAST('2026-03-29 01:30' AS DATETIME2(0)) AT TIME ZONE 'W. Europe Standard Time' AS before_gap,
       CAST('2026-03-29 02:30' AS DATETIME2(0)) AT TIME ZONE 'W. Europe Standard Time' AS in_gap,
       CAST('2026-10-25 02:30' AS DATETIME2(0)) AT TIME ZONE 'W. Europe Standard Time' AS ambiguous;
GO
DECLARE @today DATE = '2026-06-20';
DECLARE @month_start DATE = DATETRUNC(MONTH, @today);
SELECT @month_start AS month_start, DATEADD(MONTH, 1, @month_start) AS next_month_start,
       COUNT(*) AS orders_this_month
FROM dbo.orders
WHERE ordered_at >= @month_start AND ordered_at < DATEADD(MONTH, 1, @month_start);
GO
DECLARE @local_start DATETIME2(0) = '2026-01-01', @local_end DATETIME2(0) = '2026-02-01';
DECLARE @utc_start DATETIME2(0) = CAST(@local_start AT TIME ZONE 'Turkey Standard Time' AT TIME ZONE 'UTC' AS DATETIME2(0)),
        @utc_end   DATETIME2(0) = CAST(@local_end   AT TIME ZONE 'Turkey Standard Time' AT TIME ZONE 'UTC' AS DATETIME2(0));
SELECT @utc_start AS utc_start, @utc_end AS utc_end,
       (SELECT COUNT(*) FROM dbo.orders WHERE ordered_at >= '2026-01-01' AND ordered_at < '2026-02-01') AS january_utc,
       (SELECT COUNT(*) FROM dbo.orders WHERE ordered_at >= @utc_start AND ordered_at < @utc_end) AS january_istanbul;
GO
DECLARE @today DATE = '2026-06-20';
DECLARE @week_start DATE = DATETRUNC(ISO_WEEK, @today);
SELECT @week_start AS week_start, DATEADD(WEEK, -52, @week_start) AS last_year_week_start,
       DATEPART(ISO_WEEK, DATEADD(WEEK, -52, @week_start)) AS last_year_iso_week,
       (SELECT COUNT(*) FROM dbo.orders
        WHERE ordered_at >= @week_start AND ordered_at < DATEADD(DAY, 7, @week_start)) AS this_week,
       (SELECT COUNT(*) FROM dbo.orders
        WHERE ordered_at >= DATEADD(WEEK, -52, @week_start) AND ordered_at < DATEADD(WEEK, -51, @week_start)) AS same_week_last_year;
GO
SELECT DATETRUNC(MONTH, ordered_at) AS order_month,
       COUNT(*) AS shipped_orders,
       CAST(AVG(DATEDIFF(MINUTE, ordered_at, shipped_at) / 60.0) AS DECIMAL(6,1)) AS avg_hours,
       MAX(DATEDIFF(HOUR, ordered_at, shipped_at)) AS max_hours,
       SUM(CASE WHEN shipped_at < DATEADD(HOUR, 48, ordered_at) THEN 1 ELSE 0 END) AS within_48h
FROM dbo.orders
WHERE shipped_at IS NOT NULL AND ordered_at >= '2026-01-01' AND ordered_at < '2026-07-01'
GROUP BY DATETRUNC(MONTH, ordered_at)
ORDER BY order_month;
