// In-memory catalog until the SQL Server post; the JSON file is loaded once at startup
import books from "#db/books.json" with { type: "json" };

export function listBooks() {
    return books;
}

export function findBook(isbn) {
    return books.find((b) => b.isbn === isbn);
}
