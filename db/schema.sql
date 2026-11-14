/*
    PostgreSQL series - shared Bookstore lab schema and data (PostgreSQL 18).
    Usage (as bookstore_owner, inside the bookstore database):
        psql -U bookstore_owner -d bookstore -f schema.sql
    Idempotent: drops and recreates the Bookstore tables in schema public.
    Deterministic (no random()): every load produces identical rows, so the row counts and
    plans quoted in the posts are reproducible. Pseudo-random values come from
        x = (n * 48271 + salt * 69621) mod (2^31 - 1),  h = x * x mod (2^31 - 1)
    Sizes: authors 5,000 / books 40,000 / customers 100,000 / orders 1,000,000 /
           order_items ~2.5 M / stock_items 40,000 / payments 1,000,000 / shipments ~955,000
*/
\set ON_ERROR_STOP on
SET client_min_messages = warning;

SELECT current_database() LIKE 'bookstore%' AS is_bookstore_db \gset
\if :is_bookstore_db
\else
    \echo 'schema.sql must run inside a bookstore database'
    \quit
\endif

DROP TABLE IF EXISTS shipments, payments, order_items, orders, stock_items, books, authors, customers CASCADE;

/* ---------- tables: snake_case, plural, <entity>_id keys ---------- */
CREATE TABLE authors
(
    author_id int GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name      text    NOT NULL,
    country   char(2) NOT NULL
);

CREATE TABLE books
(
    book_id        int GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    isbn           varchar(13)   NOT NULL,
    title          text          NOT NULL,
    author_id      int           NOT NULL,
    category       text          NOT NULL,
    price          numeric(10,2) NOT NULL,
    published_year smallint      NOT NULL
);

CREATE TABLE customers
(
    customer_id int GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    first_name  text        NOT NULL,
    last_name   text        NOT NULL,
    email       text        NOT NULL,
    country     char(2)     NOT NULL,
    city        text        NOT NULL,
    created_at  timestamptz NOT NULL
);

CREATE TABLE orders
(
    order_id     int GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    order_number varchar(20)   NOT NULL,
    customer_id  int           NOT NULL,
    status       text          NOT NULL,
    created_at   timestamptz   NOT NULL,
    total_amount numeric(12,2) NOT NULL,
    currency     char(3)       NOT NULL
);

CREATE TABLE order_items
(
    order_item_id int GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    order_id      int           NOT NULL,
    book_id       int           NOT NULL,
    quantity      smallint      NOT NULL,
    unit_price    numeric(10,2) NOT NULL
);

CREATE TABLE stock_items
(
    book_id           int         PRIMARY KEY,
    quantity_on_hand  int         NOT NULL,
    quantity_reserved int         NOT NULL,
    updated_at        timestamptz NOT NULL
);

CREATE TABLE payments
(
    payment_id int GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    order_id   int           NOT NULL,
    amount     numeric(12,2) NOT NULL,
    currency   char(3)       NOT NULL,
    method     text          NOT NULL,
    status     text          NOT NULL,
    created_at timestamptz   NOT NULL
);

CREATE TABLE shipments
(
    shipment_id     int GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    order_id        int         NOT NULL,
    carrier         text        NOT NULL,
    tracking_number text        NOT NULL,
    shipped_at      timestamptz NOT NULL,
    delivered_at    timestamptz NULL
);

/* ---------- deterministic hash ---------- */
CREATE OR REPLACE FUNCTION pg_temp.h(n bigint, salt int) RETURNS bigint
LANGUAGE sql IMMUTABLE
AS $$ SELECT ((n * 48271 + salt * 69621) % 2147483647) * ((n * 48271 + salt * 69621) % 2147483647) % 2147483647 $$;

/* reference lists: people names and classic titles (see BOOKSTORE.md) */
CREATE TEMP TABLE ref_first_names (i int PRIMARY KEY, name text NOT NULL);
INSERT INTO ref_first_names
SELECT ord - 1, v FROM unnest(ARRAY['Ada','Alan','Grace','Linus','Barbara','Edsger','Donald','Margaret','Ken','Dennis',
    'Frances','John','Katherine','Niklaus','Radia','Tim','Hedy','Bjarne','Anita','Guido']) WITH ORDINALITY AS t(v, ord);

CREATE TEMP TABLE ref_last_names (i int PRIMARY KEY, name text NOT NULL);
INSERT INTO ref_last_names
SELECT ord - 1, v FROM unnest(ARRAY['Lovelace','Turing','Hopper','Torvalds','Liskov','Dijkstra','Knuth','Hamilton',
    'Thompson','Ritchie','Allen','Backus','Johnson','Wirth','Perlman','Berners-Lee','Lamarr','Stroustrup','Borg',
    'van Rossum']) WITH ORDINALITY AS t(v, ord);

CREATE TEMP TABLE ref_titles (i int PRIMARY KEY, title text NOT NULL, category text NOT NULL);
INSERT INTO ref_titles VALUES
    (0,'Dune','SciFi'),(1,'Emma','Classic'),(2,'The Hobbit','Fantasy'),(3,'Middlemarch','Classic'),
    (4,'Frankenstein','Classic'),(5,'Foundation','SciFi'),(6,'Persuasion','Classic'),
    (7,'The Time Machine','SciFi'),(8,'Moby-Dick','Classic'),(9,'Jane Eyre','Classic'),
    (10,'Dracula','Horror'),(11,'Ulysses','Classic'),(12,'War and Peace','Classic'),
    (13,'Don Quixote','Classic'),(14,'The Odyssey','Classic'),(15,'Neuromancer','SciFi'),
    (16,'Hyperion','SciFi'),(17,'The Silmarillion','Fantasy'),(18,'Little Women','Classic'),
    (19,'Brave New World','SciFi');

CREATE TEMP TABLE ref_cities (country char(2) NOT NULL, i int NOT NULL, city text NOT NULL, PRIMARY KEY (country, i));
INSERT INTO ref_cities VALUES
    ('TR',0,'Istanbul'),('TR',1,'Ankara'),('TR',2,'Izmir'),
    ('DE',0,'Berlin'),('DE',1,'Munich'),('DE',2,'Hamburg'),
    ('GB',0,'London'),('GB',1,'Manchester'),('GB',2,'Leeds'),
    ('NL',0,'Amsterdam'),('NL',1,'Rotterdam'),('NL',2,'Utrecht'),
    ('FR',0,'Paris'),('FR',1,'Lyon'),('FR',2,'Lille');

/* ---------- authors: 5,000 ---------- */
INSERT INTO authors (author_id, name, country) OVERRIDING SYSTEM VALUE
SELECT g.n, f.name || ' ' || l.name,
       (ARRAY['GB','US','FR','DE','TR'])[1 + (r.h / 400) % 5]
FROM generate_series(1, 5000) AS g(n)
CROSS JOIN LATERAL (SELECT pg_temp.h(g.n, 1) AS h) AS r
JOIN ref_first_names AS f ON f.i = r.h % 20
JOIN ref_last_names  AS l ON l.i = (r.h / 20) % 20
ORDER BY g.n;

/* ---------- books: 40,000; price 5.00 .. 49.99, 20 classic titles x 2,000 ---------- */
INSERT INTO books (book_id, isbn, title, author_id, category, price, published_year) OVERRIDING SYSTEM VALUE
SELECT g.n,
       '978' || lpad(g.n::text, 10, '0'),
       t.title || ' #' || g.n,
       1 + r.h % 5000,
       t.category,
       5 + ((r.h / 5000) % 4500) / 100.0,
       1850 + (r.h / 22500000) % 76 + (r.h % 7) * 15
FROM generate_series(1, 40000) AS g(n)
CROSS JOIN LATERAL (SELECT pg_temp.h(g.n, 2) AS h) AS r
JOIN ref_titles AS t ON t.i = g.n % 20
ORDER BY g.n;

/* ---------- customers: 100,000; country skewed (TR 60%), city correlated with country ---------- */
INSERT INTO customers (customer_id, first_name, last_name, email, country, city, created_at) OVERRIDING SYSTEM VALUE
SELECT c.n, f.name, l.name, 'customer' || c.n || '@example.com', c.country, ci.city,
       timestamptz '2021-01-01 00:00:00+00' + c.n * interval '9 minutes'
FROM
(
    SELECT g.n, r.h,
           CASE WHEN g.n = 1 THEN 'TR'
                WHEN r.h % 100 < 60 THEN 'TR'
                WHEN r.h % 100 < 75 THEN 'DE'
                WHEN r.h % 100 < 85 THEN 'GB'
                WHEN r.h % 100 < 93 THEN 'NL'
                ELSE 'FR' END AS country,
           CASE WHEN (r.h / 100) % 10 < 7 THEN 0
                WHEN (r.h / 100) % 10 < 9 THEN 1
                ELSE 2 END AS city_index
    FROM generate_series(1, 100000) AS g(n)
    CROSS JOIN LATERAL (SELECT pg_temp.h(g.n, 4) AS h) AS r
) AS c
JOIN ref_first_names AS f  ON f.i = (c.h / 1000) % 20
JOIN ref_last_names  AS l  ON l.i = (c.h / 20000) % 20
JOIN ref_cities      AS ci ON ci.country = c.country AND ci.i = c.city_index
ORDER BY c.n;

/* customer 1 is the big B2B account (a university library) */
UPDATE customers SET first_name = 'Grace', last_name = 'Hopper', city = 'Istanbul' WHERE customer_id = 1;

/* ---------- order_items: 1..4 lines per order, book popularity skewed (inserted before orders,
               so orders.total_amount is written once and the table has no dead rows) ---------- */
INSERT INTO order_items (order_id, book_id, quantity, unit_price)
SELECT x.order_id, b.book_id,
       CASE WHEN x.h % 50 = 1 THEN 3 WHEN x.h % 10 = 0 THEN 2 ELSE 1 END,
       b.price
FROM
(
    SELECT o.n AS order_id, v.i, ri.h,
           1 + floor(39999 * power(ri.h::float8 / 2147483647, 2))::int AS book_id
    FROM generate_series(1, 1000000) AS o(n)
    CROSS JOIN LATERAL (SELECT pg_temp.h(o.n, 6) AS h) AS ro
    JOIN generate_series(1, 4) AS v(i) ON v.i <= 1 + ro.h % 4
    CROSS JOIN LATERAL (SELECT pg_temp.h(o.n::bigint * 4 + v.i, 7) AS h) AS ri
) AS x
JOIN books AS b ON b.book_id = x.book_id
ORDER BY x.order_id, x.i;

/* ---------- orders: 1,000,000; customer_id heavily skewed, created_at ascending with order_id ---------- */
INSERT INTO orders (order_id, order_number, customer_id, status, created_at, total_amount, currency) OVERRIDING SYSTEM VALUE
SELECT o.n,
       'BK-' || lpad(o.n::text, 8, '0'),
       cu.customer_id,
       CASE WHEN o.n % 25 = 7 THEN 'Cancelled'
            WHEN o.n > 1000000 - 5000 THEN 'Pending'
            WHEN o.n > 1000000 - 50000 AND o.n % 97 = 0 THEN 'PaymentPending'
            WHEN o.n > 1000000 - 20000 THEN 'Shipped'
            ELSE 'Delivered' END,
       timestamptz '2023-01-01 00:00:00+00' + (o.n - 1) * interval '120 seconds',
       t.total,
       CASE WHEN cu.country = 'TR' THEN 'TRY' ELSE 'EUR' END
FROM
(
    SELECT g.n,
           CASE WHEN g.n % 5 = 0 THEN 1
                ELSE 2 + floor(99999 * power(r.h::float8 / 2147483647, 3))::int
           END AS customer_id
    FROM generate_series(1, 1000000) AS g(n)
    CROSS JOIN LATERAL (SELECT pg_temp.h(g.n, 5) AS h) AS r
) AS o
JOIN customers AS cu ON cu.customer_id = least(o.customer_id, 100000)
JOIN (SELECT order_id, sum(quantity * unit_price) AS total FROM order_items GROUP BY order_id) AS t
  ON t.order_id = o.n
ORDER BY o.n;

/* ---------- stock_items: one row per book ---------- */
INSERT INTO stock_items (book_id, quantity_on_hand, quantity_reserved, updated_at)
SELECT b.book_id, pg_temp.h(b.book_id, 9) % 200, 0, timestamptz '2026-10-31 00:00:00+00'
FROM books AS b
ORDER BY b.book_id;

/* ---------- payments: one per order, payment_id = order_id ---------- */
INSERT INTO payments (payment_id, order_id, amount, currency, method, status, created_at) OVERRIDING SYSTEM VALUE
SELECT o.order_id, o.order_id, o.total_amount, o.currency,
       CASE WHEN r.h % 10 < 6 THEN 'Card' WHEN r.h % 10 < 9 THEN 'BankTransfer' ELSE 'Wallet' END,
       CASE o.status WHEN 'Cancelled' THEN 'Refunded'
                     WHEN 'PaymentPending' THEN 'Authorized'
                     WHEN 'Pending' THEN 'Authorized'
                     ELSE 'Captured' END,
       o.created_at + interval '2 minutes'
FROM orders AS o
CROSS JOIN LATERAL (SELECT pg_temp.h(o.order_id, 10) AS h) AS r
ORDER BY o.order_id;

/* ---------- shipments: for Shipped and Delivered orders ---------- */
INSERT INTO shipments (order_id, carrier, tracking_number, shipped_at, delivered_at)
SELECT o.order_id,
       CASE WHEN o.order_id % 3 = 0 THEN 'Express' ELSE 'Standard' END,
       'TRK' || lpad(o.order_id::text, 10, '0'),
       o.created_at + (24 + r.h % 48) * interval '1 hour',
       CASE WHEN o.status = 'Delivered' THEN o.created_at + (72 + (r.h / 48) % 96) * interval '1 hour' END
FROM orders AS o
CROSS JOIN LATERAL (SELECT pg_temp.h(o.order_id, 11) AS h) AS r
WHERE o.status IN ('Shipped', 'Delivered')
ORDER BY o.order_id;

/* identity columns continue after the explicitly inserted ids */
DO $$
BEGIN
    PERFORM setval(pg_get_serial_sequence('authors', 'author_id'),     (SELECT max(author_id)   FROM authors));
    PERFORM setval(pg_get_serial_sequence('books', 'book_id'),         (SELECT max(book_id)     FROM books));
    PERFORM setval(pg_get_serial_sequence('customers', 'customer_id'), (SELECT max(customer_id) FROM customers));
    PERFORM setval(pg_get_serial_sequence('orders', 'order_id'),       (SELECT max(order_id)    FROM orders));
    PERFORM setval(pg_get_serial_sequence('payments', 'payment_id'),   (SELECT max(payment_id)  FROM payments));
END
$$;

/* ---------- constraints and the indexes EF Core conventions would create ---------- */
CREATE UNIQUE INDEX ux_books_isbn          ON books (isbn);
CREATE INDEX        ix_books_author_id     ON books (author_id);
CREATE UNIQUE INDEX ux_customers_email     ON customers (email);
CREATE UNIQUE INDEX ux_orders_order_number ON orders (order_number);
CREATE INDEX        ix_orders_customer_id  ON orders (customer_id);
CREATE INDEX        ix_order_items_order_id ON order_items (order_id);
CREATE INDEX        ix_order_items_book_id ON order_items (book_id);
CREATE UNIQUE INDEX ux_payments_order_id   ON payments (order_id);
CREATE UNIQUE INDEX ux_shipments_order_id  ON shipments (order_id);

ALTER TABLE books       ADD CONSTRAINT fk_books_authors       FOREIGN KEY (author_id)   REFERENCES authors (author_id);
ALTER TABLE orders      ADD CONSTRAINT fk_orders_customers    FOREIGN KEY (customer_id) REFERENCES customers (customer_id);
ALTER TABLE order_items ADD CONSTRAINT fk_order_items_orders  FOREIGN KEY (order_id)    REFERENCES orders (order_id);
ALTER TABLE order_items ADD CONSTRAINT fk_order_items_books   FOREIGN KEY (book_id)     REFERENCES books (book_id);
ALTER TABLE stock_items ADD CONSTRAINT fk_stock_items_books   FOREIGN KEY (book_id)     REFERENCES books (book_id);
ALTER TABLE payments    ADD CONSTRAINT fk_payments_orders     FOREIGN KEY (order_id)    REFERENCES orders (order_id);
ALTER TABLE shipments   ADD CONSTRAINT fk_shipments_orders    FOREIGN KEY (order_id)    REFERENCES orders (order_id);
ALTER TABLE orders      ADD CONSTRAINT ck_orders_status
    CHECK (status IN ('Pending', 'PaymentPending', 'Shipped', 'Delivered', 'Cancelled'));
ALTER TABLE orders      ADD CONSTRAINT ck_orders_currency CHECK (currency IN ('EUR', 'TRY'));
ALTER TABLE stock_items ADD CONSTRAINT ck_stock_items_quantity CHECK (quantity_on_hand >= 0 AND quantity_reserved >= 0);

VACUUM (ANALYZE) authors, books, customers, orders, order_items, stock_items, payments, shipments;

SELECT relname AS table_name, n_live_tup AS rows, pg_size_pretty(pg_total_relation_size(relid)) AS total_size
FROM pg_stat_user_tables
WHERE schemaname = 'public'
ORDER BY relname;
