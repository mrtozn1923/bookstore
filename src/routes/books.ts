import type { IncomingMessage, ServerResponse } from "node:http";
import { toBookSummary } from "@bookstore/contracts";
import { listBooks, findBook } from "#services/bookService";
import { HttpError } from "#http/errors";
import { sendJson } from "#http/respond";
import type { Params } from "#http/router";

// GET /api/books
export function handleListBooks(req: IncomingMessage, res: ServerResponse): void {
    sendJson(res, 200, listBooks().map(toBookSummary));
}

// GET /api/books/:isbn
export function handleGetBook(req: IncomingMessage, res: ServerResponse, { params }: { params: Params }): void {
    const book = findBook(params.isbn);
    if (!book) {
        throw new HttpError(404, `book ${params.isbn} not found`);
    }
    sendJson(res, 200, toBookSummary(book));
}
