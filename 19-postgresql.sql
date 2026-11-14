SELECT length('Dune   ') AS length_text, length('Dune   '::char(7)) AS length_char,
       octet_length('Kürk') AS bytes_utf8, position('e' IN 'Middlemarch') AS first_e,
       split_part('Ursula K. Le Guin', ' ', -1) AS last_part,
       overlay('9798900000268' PLACING '-' FROM 4 FOR 0) AS overlay_insert,
       translate('Kürk Mantolu', 'ü', 'u') AS translated;
SELECT replace('Dune Messiah', 'd', 'X') AS replace_cs;
SELECT concat_ws(', ', NULL, NULL) AS ws_all_null, 'Dune' || NULL AS pipe_null;
SELECT value, ordinal FROM unnest(string_to_array('12,7,,42, 26', ',')) WITH ORDINALITY AS t(value, ordinal);
SELECT string_agg(x, '; ' ORDER BY x) AS agg FROM (VALUES ('Pratchett'), ('Gaiman'), (NULL)) AS v(x);
SELECT quote_ident('order items') AS quoted, upper('istanbul') AS upper_default;
