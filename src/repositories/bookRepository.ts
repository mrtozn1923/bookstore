import type { Book } from "#domain/book";

export type BookSort = "title" | "price" | "-price";

export interface BookQuery {
    search?: string;
    author?: string;
    sort: BookSort;
    offset: number;
    limit: number;
}

// Async on purpose: the SQL Server implementation in post 13 keeps the same interface
export interface BookRepository {
    list(query: BookQuery): Promise<{ items: Book[]; totalCount: number }>;
    findByIsbn(isbn: string): Promise<Book | undefined>;
    findManyByIsbn(isbns: readonly string[]): Promise<Map<string, Book>>;
}

export class InMemoryBookRepository implements BookRepository {
    readonly #books: readonly Book[];

    constructor(books: readonly Book[]) {
        this.#books = books;
    }

    async list({ search, author, sort, offset, limit }: BookQuery): Promise<{ items: Book[]; totalCount: number }> {
        const term = search?.toLocaleLowerCase("en");
        const filtered = this.#books
            .filter((b) => !term || b.title.toLocaleLowerCase("en").includes(term))
            .filter((b) => !author || b.author === author);
        const sorted = filtered.toSorted((a, b) =>
            sort === "title" ? a.title.localeCompare(b.title, "en") : sort === "price" ? a.priceMinor - b.priceMinor : b.priceMinor - a.priceMinor,
        );
        return { items: sorted.slice(offset, offset + limit), totalCount: filtered.length };
    }

    async findByIsbn(isbn: string): Promise<Book | undefined> {
        return this.#books.find((b) => b.isbn === isbn);
    }

    async findManyByIsbn(isbns: readonly string[]): Promise<Map<string, Book>> {
        return new Map(this.#books.filter((b) => isbns.includes(b.isbn)).map((b) => [b.isbn, b]));
    }
}
