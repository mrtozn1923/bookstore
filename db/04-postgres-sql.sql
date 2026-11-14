/*
    Post 04 - PostgreSQL-specific SQL and constraints. Run as bookstore_owner right after schema.sql:
        psql -U bookstore_owner -d bookstore -f /lab/04-postgres-sql.sql
    Some statements fail on purpose, so ON_ERROR_STOP is off. Data changes are rolled back,
    except the new tables cart_items, campaigns, book_prices and campaign_items.
*/
\set ON_ERROR_STOP off
DROP TABLE IF EXISTS campaign_items, book_prices, campaigns, cart_items;

/* RETURNING and RETURNING OLD/NEW */
BEGIN;
INSERT INTO authors (name, country) VALUES ('Ursula K. Le Guin', 'US') RETURNING author_id, name;
UPDATE stock_items
SET quantity_on_hand = quantity_on_hand - 2,
    quantity_reserved = quantity_reserved + 2,
    updated_at = now()
WHERE book_id = 1 AND quantity_on_hand >= 2
RETURNING book_id, old.quantity_on_hand AS before, new.quantity_on_hand AS after, new.quantity_reserved AS reserved;
UPDATE stock_items
SET quantity_on_hand = quantity_on_hand - 500
WHERE book_id = 1 AND quantity_on_hand >= 500
RETURNING book_id, old.quantity_on_hand AS before, new.quantity_on_hand AS after;
ROLLBACK;

/* ON CONFLICT */
CREATE TABLE cart_items
(
    cart_id  int NOT NULL,
    book_id  int NOT NULL REFERENCES books (book_id),
    quantity int NOT NULL CHECK (quantity > 0),
    added_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (cart_id, book_id)
);
INSERT INTO cart_items (cart_id, book_id, quantity) VALUES (1, 7, 1)
ON CONFLICT (cart_id, book_id) DO UPDATE SET quantity = cart_items.quantity + EXCLUDED.quantity
RETURNING book_id, old.quantity AS old_qty, new.quantity AS new_qty, old IS NULL AS inserted;
INSERT INTO cart_items (cart_id, book_id, quantity) VALUES (1, 7, 2)
ON CONFLICT (cart_id, book_id) DO UPDATE SET quantity = cart_items.quantity + EXCLUDED.quantity
RETURNING book_id, old.quantity AS old_qty, new.quantity AS new_qty, old IS NULL AS inserted;
INSERT INTO cart_items (cart_id, book_id, quantity) VALUES (1, 7, 5)
ON CONFLICT (cart_id, book_id) DO NOTHING
RETURNING book_id, quantity;
INSERT INTO cart_items (cart_id, book_id, quantity) VALUES (1, 7, 5)
ON CONFLICT (book_id) DO NOTHING;
INSERT INTO cart_items (cart_id, book_id, quantity) VALUES (1, 2, 1), (1, 2, 3)
ON CONFLICT (cart_id, book_id) DO UPDATE SET quantity = cart_items.quantity + EXCLUDED.quantity;

/* MERGE ... RETURNING (17+) */
CREATE TEMP TABLE price_feed (isbn varchar(13), price numeric(10,2));
INSERT INTO price_feed VALUES ('9780000000001', 29.90), ('9780000000002', 39.00), ('9780000099999', 12.50);
BEGIN;
MERGE INTO books AS b
USING price_feed AS f ON b.isbn = f.isbn
WHEN MATCHED AND b.price <> f.price THEN UPDATE SET price = f.price
WHEN MATCHED THEN DO NOTHING
WHEN NOT MATCHED THEN INSERT (isbn, title, author_id, category, price, published_year)
                      VALUES (f.isbn, 'Unknown #' || f.isbn, 1, 'Classic', f.price, 2026)
RETURNING merge_action() AS action, b.book_id, f.isbn, old.price AS old_price, new.price AS new_price;
ROLLBACK;

/* DISTINCT ON, LATERAL, generate_series, FILTER */
SELECT DISTINCT ON (category) category, book_id, title, published_year
FROM books
ORDER BY category, published_year DESC, book_id;

SELECT c.customer_id, c.first_name, o.order_number, o.created_at, o.total_amount
FROM customers AS c
CROSS JOIN LATERAL (
    SELECT order_number, created_at, total_amount
    FROM orders
    WHERE orders.customer_id = c.customer_id
    ORDER BY created_at DESC
    LIMIT 2
) AS o
WHERE c.customer_id IN (2, 3)
ORDER BY c.customer_id, o.created_at DESC;

SELECT d::date AS day,
       count(o.order_id) AS orders,
       count(o.order_id) FILTER (WHERE o.status = 'Cancelled') AS cancelled,
       coalesce(sum(o.total_amount) FILTER (WHERE o.status <> 'Cancelled'), 0) AS revenue
FROM generate_series(timestamptz '2026-10-18', timestamptz '2026-10-22', interval '1 day') AS d
LEFT JOIN orders AS o ON o.created_at >= d AND o.created_at < d + interval '1 day'
GROUP BY d
ORDER BY d;

/* writable CTE: archive and delete in one statement */
BEGIN;
CREATE TABLE shipments_archive (LIKE shipments);
WITH archived AS (
    DELETE FROM shipments
    WHERE delivered_at < timestamptz '2023-02-01'
    RETURNING *
), inserted AS (
    INSERT INTO shipments_archive SELECT * FROM archived
    RETURNING shipment_id
)
SELECT count(*) AS moved, min(shipment_id) AS first_id, max(shipment_id) AS last_id FROM inserted;
SELECT (SELECT count(*) FROM shipments) AS live, (SELECT count(*) FROM shipments_archive) AS archived;
ROLLBACK;

/* deferrable foreign key */
ALTER TABLE order_items ALTER CONSTRAINT fk_order_items_orders DEFERRABLE INITIALLY IMMEDIATE;
BEGIN;
SET CONSTRAINTS fk_order_items_orders DEFERRED;
INSERT INTO order_items (order_id, book_id, quantity, unit_price) VALUES (1000001, 1, 1, 32.77);
INSERT INTO orders (order_id, order_number, customer_id, status, created_at, total_amount, currency)
OVERRIDING SYSTEM VALUE VALUES (1000001, 'BK-01000001', 42, 'Pending', now(), 32.77, 'TRY');
SET CONSTRAINTS fk_order_items_orders IMMEDIATE;
ROLLBACK;
BEGIN;
SET CONSTRAINTS fk_order_items_orders DEFERRED;
INSERT INTO order_items (order_id, book_id, quantity, unit_price) VALUES (1000002, 1, 1, 32.77);
COMMIT;
ALTER TABLE order_items ALTER CONSTRAINT fk_order_items_orders NOT DEFERRABLE;

/* EXCLUDE: no two campaigns of the same category may overlap (btree_gist is a trusted extension) */
CREATE TABLE campaigns
(
    campaign_id int GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name        text NOT NULL,
    category    text NOT NULL,
    discount    numeric(4,2) NOT NULL,
    period      daterange NOT NULL
);
DROP EXTENSION IF EXISTS btree_gist;
ALTER TABLE campaigns ADD CONSTRAINT ex_campaigns_category_period
    EXCLUDE USING gist (category WITH =, period WITH &&);
CREATE EXTENSION btree_gist;
ALTER TABLE campaigns ADD CONSTRAINT ex_campaigns_category_period
    EXCLUDE USING gist (category WITH =, period WITH &&);
INSERT INTO campaigns (name, category, discount, period) VALUES
    ('Back to School', 'Classic', 0.10, '[2026-09-01,2026-09-15)'),
    ('Black Friday',   'SciFi',   0.25, '[2026-11-27,2026-11-30]'),
    ('Winter Reads',   'Classic', 0.15, '[2026-12-01,2027-01-01)');
INSERT INTO campaigns (name, category, discount, period)
VALUES ('Cyber Monday', 'SciFi', 0.30, '[2026-11-30,2026-12-02)');

/* temporal primary key and foreign key (18) */
CREATE TABLE book_prices
(
    book_id      int NOT NULL REFERENCES books (book_id),
    valid_during daterange NOT NULL,
    price        numeric(10,2) NOT NULL,
    PRIMARY KEY (book_id, valid_during WITHOUT OVERLAPS)
);
INSERT INTO book_prices VALUES
    (1, '[2026-01-01,2026-07-01)', 32.77),
    (1, '[2026-07-01,2027-01-01)', 34.90);
INSERT INTO book_prices VALUES (1, '[2026-12-01,2027-02-01)', 36.00);
SELECT indexdef FROM pg_indexes WHERE indexname = 'book_prices_pkey';

CREATE TABLE campaign_items
(
    campaign_id  int NOT NULL REFERENCES campaigns (campaign_id),
    book_id      int NOT NULL,
    valid_during daterange NOT NULL,
    PRIMARY KEY (campaign_id, book_id),
    FOREIGN KEY (book_id, PERIOD valid_during) REFERENCES book_prices (book_id, PERIOD valid_during)
);
INSERT INTO campaign_items VALUES (1, 1, '[2026-06-15,2026-07-15)');
INSERT INTO campaign_items VALUES (3, 1, '[2026-12-20,2027-01-10)');

/* NOT NULL ... NOT VALID (18) */
BEGIN;
ALTER TABLE customers ADD COLUMN phone text;
UPDATE customers SET phone = '+90 555 000 ' || lpad(customer_id::text, 4, '0') WHERE customer_id <= 10;
ALTER TABLE customers ADD CONSTRAINT customers_phone_not_null NOT NULL phone NOT VALID;
SAVEPOINT before_insert;
INSERT INTO customers (first_name, last_name, email, country, city, created_at)
VALUES ('Ada', 'Lovelace', 'new-ada@example.com', 'GB', 'London', now());
ROLLBACK TO SAVEPOINT before_insert;
SELECT count(*) FILTER (WHERE phone IS NULL) AS old_rows_without_phone FROM customers;
SAVEPOINT before_validate;
ALTER TABLE customers VALIDATE CONSTRAINT customers_phone_not_null;
ROLLBACK TO SAVEPOINT before_validate;
SELECT conname, convalidated FROM pg_constraint WHERE conname = 'customers_phone_not_null';
ROLLBACK;

/* generated columns: virtual (default in 18) and stored */
CREATE TABLE gen_demo AS SELECT order_item_id, order_id, quantity, unit_price FROM order_items WHERE order_id <= 100000;
SELECT pg_relation_filenode('gen_demo') AS filenode;
ALTER TABLE gen_demo ADD COLUMN line_total numeric(12,2) GENERATED ALWAYS AS (quantity * unit_price);
SELECT pg_relation_filenode('gen_demo') AS filenode_after_virtual;
ALTER TABLE gen_demo ADD COLUMN line_total_stored numeric(12,2) GENERATED ALWAYS AS (quantity * unit_price) STORED;
SELECT pg_relation_filenode('gen_demo') AS filenode_after_stored;
SELECT order_item_id, quantity, unit_price, line_total, line_total_stored FROM gen_demo WHERE quantity = 3 LIMIT 2;
CREATE INDEX ix_gen_demo_line_total ON gen_demo (line_total);
CREATE INDEX ix_gen_demo_line_total_stored ON gen_demo (line_total_stored);
DROP TABLE gen_demo;
