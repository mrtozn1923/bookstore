import { Router } from "express";
import type { BookService } from "#services/bookService";
import { listBooks, getBook } from "#handlers/booksHandlers";

// Routes only: which path goes to which handler
export function booksRouter(books: BookService): Router {
    return Router().get("/", listBooks(books)).get("/:isbn", getBook(books));
}
