import type { RequestHandler } from "express";
import type { BookService } from "#services/bookService";
import { listBooksQuery, bookParams } from "#schemas/books";
import { parseRequest } from "#http/validation";
import { toBookDto } from "#handlers/dto";

// GET /api/books?page=1&pageSize=10&q=...&author=...&sort=title|price|-price
export function listBooks(books: BookService): RequestHandler {
    return async (req, res) => {
        const { q, ...query } = parseRequest(listBooksQuery, req.query);
        const page = await books.list({ ...query, search: q });
        res.json({ ...page, items: page.items.map(toBookDto) });
    };
}

// GET /api/books/:isbn
export function getBook(books: BookService): RequestHandler {
    return async (req, res) => {
        const { isbn } = parseRequest(bookParams, req.params);
        res.json(toBookDto(await books.get(isbn)));
    };
}
