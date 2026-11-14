import { Router } from "express";
import { toBookSummary } from "@bookstore/contracts";
import { listBooks, findBook } from "#services/bookService";
import { HttpError } from "#http/errors";

export const booksRouter = Router();

// GET /api/books
booksRouter.get("/", (req, res) => {
    res.json(listBooks().map(toBookSummary));
});

// GET /api/books/:isbn
booksRouter.get("/:isbn", (req, res) => {
    const book = findBook(req.params.isbn);
    if (!book) {
        throw new HttpError(404, `book ${req.params.isbn} not found`);
    }
    res.json(toBookSummary(book));
});
