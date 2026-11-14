SELECT round(2.345, 2) AS r2, trunc(2.345, 2) AS truncated, round(-2.5) AS neg_half, round(2.5::float8) AS float_half,
       7 / 2 AS int_div, 7 / 2.0 AS dec_div, div(7, 2) AS div_fn, power(2, 0.5) AS pow;
SELECT gen_random_uuid() IS NOT NULL AS v4_ok, uuid_extract_version(uuidv7()) AS v7_version;
SELECT avg(x) AS avg_int_input FROM (VALUES (4), (5)) AS v(x);
SELECT to_regclass('public.no_such_table') AS missing;
CREATE TABLE t22 (id int GENERATED ALWAYS AS IDENTITY PRIMARY KEY, v text);
INSERT INTO t22 (v) VALUES ('a'), ('b') RETURNING id;
SELECT lastval() AS last_value;
DO $$ DECLARE n bigint; BEGIN UPDATE t22 SET v = v; GET DIAGNOSTICS n = ROW_COUNT; RAISE NOTICE 'rows=%', n; END $$;
SELECT count(DISTINCT random()) AS distinct_random FROM generate_series(1, 3);
SELECT column_name, data_type, character_maximum_length FROM information_schema.columns WHERE table_name = 't22' ORDER BY ordinal_position;
SELECT relname FROM pg_catalog.pg_class WHERE relname = 't22';
SELECT to_regclass('public.t22') AS existing;
DROP TABLE t22;
