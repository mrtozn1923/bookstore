// In-memory catalog until the SQL Server post; the JSON file is loaded once at startup
import type { Book } from "#domain/book";
import books from "#db/books.json" with { type: "json" };

const catalog: readonly Book[] = books as Book[];

export function listBooks(): readonly Book[] {
    return catalog;
}

export function findBook(isbn: string): Book | undefined {
    return catalog.find((b) => b.isbn === isbn);
}
