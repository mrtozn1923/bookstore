CREATE TABLE book_views (
    book_id   int NOT NULL,
    viewed_at timestamptz NOT NULL DEFAULT now()
);

CREATE FUNCTION book_details(p_book_id int)
RETURNS TABLE (title text, price numeric)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO book_views (book_id) VALUES (p_book_id);
    RETURN QUERY SELECT b.title, b.price FROM books AS b WHERE b.book_id = p_book_id;
END
$$;
