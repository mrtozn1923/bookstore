export interface Book {
    isbn: string;
    title: string;
}

export function describe(book: Book): string {
    return `${book.title} (${book.isbn})`;
}
