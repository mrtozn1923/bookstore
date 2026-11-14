import type { IncomingMessage, ServerResponse } from "node:http";
import { toBookSummary } from "@bookstore/contracts";
import { listBooks } from "#services/bookService";

// GET /api/books
export function handleListBooks(req: IncomingMessage, res: ServerResponse): void {
    res.writeHead(200, { "content-type": "application/json" });
    res.end(JSON.stringify(listBooks().map(toBookSummary)));
}
