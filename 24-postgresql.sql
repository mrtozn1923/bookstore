CREATE TABLE books24 (book_id int PRIMARY KEY, list_price numeric(10,2) NOT NULL CHECK (list_price >= 0));
INSERT INTO books24 VALUES (26, 390.50), (28, 611.90);
BEGIN;
UPDATE books24 SET list_price = list_price + 10 WHERE book_id = 26;
UPDATE books24 SET list_price = -1 WHERE book_id = 28;
SELECT list_price FROM books24 WHERE book_id = 26;
ROLLBACK;
DO $$
BEGIN
    UPDATE books24 SET list_price = list_price + 10 WHERE book_id = 26;
    UPDATE books24 SET list_price = -1 WHERE book_id = 28;
EXCEPTION WHEN check_violation THEN
    RAISE NOTICE 'caught: %', SQLERRM;
END $$;
SELECT book_id, list_price FROM books24 ORDER BY book_id;
DROP TABLE books24;
