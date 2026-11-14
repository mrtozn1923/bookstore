import type { Book } from "#domain/book";
import { BookNotFoundError } from "#domain/errors";
import type { BookRepository, BookSort } from "#repositories/bookRepository";

export interface Page<T> {
    items: T[];
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
    hasNextPage: boolean;
}

export class BookService {
    readonly #books: BookRepository;

    constructor(books: BookRepository) {
        this.#books = books;
    }

    async list(options: { page: number; pageSize: number; search?: string; author?: string; sort: BookSort }): Promise<Page<Book>> {
        const { page, pageSize, ...filter } = options;
        const { items, totalCount } = await this.#books.list({ ...filter, offset: (page - 1) * pageSize, limit: pageSize });
        const totalPages = Math.ceil(totalCount / pageSize);
        return { items, page, pageSize, totalCount, totalPages, hasNextPage: page < totalPages };
    }

    async get(isbn: string): Promise<Book> {
        const book = await this.#books.findByIsbn(isbn);
        if (!book) {
            throw new BookNotFoundError(isbn);
        }
        return book;
    }
}
