-- PostgreSQL schema for the lab: the catalog part of BookstoreDb
DROP TABLE IF EXISTS books, authors, categories;

CREATE TABLE categories (
    id serial PRIMARY KEY,
    slug text NOT NULL UNIQUE,
    name text NOT NULL
);

CREATE TABLE authors (
    id serial PRIMARY KEY,
    name text NOT NULL
);

CREATE TABLE books (
    isbn char(13) PRIMARY KEY,
    title text NOT NULL,
    author_id int NOT NULL REFERENCES authors (id),
    category_id int NOT NULL REFERENCES categories (id),
    price numeric(10, 2) NOT NULL,
    currency char(3) NOT NULL,
    stock bigint NOT NULL DEFAULT 0,
    added_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_books_category ON books (category_id, title);

INSERT INTO categories (slug, name) VALUES ('classics', 'Classics'), ('science-fiction', 'Science Fiction'), ('fantasy', 'Fantasy');
INSERT INTO authors (name) VALUES ('Jane Austen'), ('George Eliot'), ('Frank Herbert'), ('J.R.R. Tolkien'), ('Mary Shelley');
INSERT INTO books (isbn, title, author_id, category_id, price, currency, stock, added_at) VALUES
    ('9780141439587', 'Emma', 1, 1, 245.00, 'TRY', 12, '2026-11-02T09:00:00Z'),
    ('9780141439518', 'Pride and Prejudice', 1, 1, 229.90, 'TRY', 30, '2026-11-02T09:00:00Z'),
    ('9780141439549', 'Middlemarch', 2, 1, 310.00, 'TRY', 7, '2026-11-03T09:00:00Z'),
    ('9780141439471', 'Frankenstein', 5, 1, 199.90, 'TRY', 0, '2026-11-04T09:00:00Z'),
    ('9780441172719', 'Dune', 3, 2, 349.90, 'TRY', 18, '2026-11-05T09:00:00Z'),
    ('9780547928227', 'The Hobbit', 4, 3, 289.50, 'TRY', 19, '2026-11-06T09:00:00Z');
