import type { Book } from "./books";

export function searchBooks(books: readonly Book[], query: string): Book[] {
    const term = query.trim().toLowerCase();
    if (term === "") {
        return [...books];
    }
    return books.filter(book =>
        book.title.toLowerCase().includes(term) || book.author.toLowerCase().includes(term));
}
