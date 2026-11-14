CREATE FUNCTION bucket_of(p_customer_id int) RETURNS int
LANGUAGE sql IMMUTABLE AS $$
    SELECT (((p_customer_id::bigint * 2654435761) % 4294967296) >> 26)::int
$$;
