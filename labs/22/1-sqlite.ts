import { DatabaseSync } from "node:sqlite";

// An in-memory database: nothing to install, gone when the process exits (tests, small tools, local caches)
const db = new DatabaseSync(":memory:");
db.exec(`
    CREATE TABLE categories (id INTEGER PRIMARY KEY, slug TEXT NOT NULL UNIQUE, name TEXT NOT NULL);
    CREATE TABLE authors (id INTEGER PRIMARY KEY, name TEXT NOT NULL);
    CREATE TABLE books (
        isbn TEXT PRIMARY KEY, title TEXT NOT NULL,
        author_id INTEGER NOT NULL REFERENCES authors (id), category_id INTEGER NOT NULL REFERENCES categories (id),
        price_minor INTEGER NOT NULL, currency TEXT NOT NULL, stock INTEGER NOT NULL DEFAULT 0
    );
`);

// Prepared statements with ? parameters; one transaction around the inserts
const insertBook = db.prepare("INSERT INTO books VALUES (?, ?, ?, ?, ?, ?, ?)");
db.exec("BEGIN");
db.prepare("INSERT INTO categories (slug, name) VALUES (?, ?), (?, ?)").run("classics", "Classics", "science-fiction", "Science Fiction");
db.prepare("INSERT INTO authors (name) VALUES (?), (?), (?)").run("Jane Austen", "George Eliot", "Frank Herbert");
insertBook.run("9780141439587", "Emma", 1, 1, 24500, "TRY", 12);
insertBook.run("9780141439518", "Pride and Prejudice", 1, 1, 22990, "TRY", 30);
insertBook.run("9780141439549", "Middlemarch", 2, 1, 31000, "TRY", 7);
insertBook.run("9780441172719", "Dune", 3, 2, 34990, "TRY", 18);
db.exec("COMMIT");

const booksByCategory = db.prepare(`
    SELECT b.isbn, b.title, a.name AS author, b.price_minor AS priceMinor, b.currency
    FROM books b
    JOIN authors a ON a.id = b.author_id
    JOIN categories c ON c.id = b.category_id
    WHERE c.slug = ?
    ORDER BY b.title
    LIMIT ?`);
const rows = booksByCategory.all("classics", 10) as { title: string; author: string; priceMinor: number; currency: string }[];
console.log(rows.map((r) => `${r.title} | ${r.author} | ${r.priceMinor} ${r.currency}`).join("\n"));

// SQLite has dynamic types: a column declared INTEGER happily stores text
insertBook.run("9780000000002", "Typo", 1, 1, "245,00", "TRY", 1);
const typo = db.prepare("SELECT price_minor, typeof(price_minor) AS stored FROM books WHERE isbn = ?").get("9780000000002");
console.log(typo);

// STRICT tables (SQLite 3.37 and later) refuse the wrong type
db.exec("CREATE TABLE prices (price_minor INTEGER NOT NULL) STRICT");
try {
    db.prepare("INSERT INTO prices VALUES (?)").run("245,00");
} catch (err) {
    console.log((err as Error).message);
}
db.close();
