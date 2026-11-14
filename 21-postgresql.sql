SELECT pg_input_is_valid('1450.00', 'numeric(10,2)') AS ok_numeric, pg_input_is_valid('N/A', 'numeric(10,2)') AS bad_numeric,
       pg_input_is_valid('2026-11-31', 'date') AS bad_date;
SELECT to_number('1.234,50', '9G999D99') AS to_number_default, to_date('03.11.2026', 'DD.MM.YYYY') AS to_date_104;
SELECT to_date('31.11.2026', 'DD.MM.YYYY');
SELECT to_char(1234.5, 'FM9G999D00') AS to_char_default, coalesce(NULL::int, 1.5) AS coalesce_mixed, nullif(0, 0) AS nullif_zero;
SELECT ''::int;
SET lc_numeric = 'C.UTF-8';
SHOW lc_numeric;
