import { listBooks } from "#services/bookService";

// GET /api/books
export function handleListBooks(req, res) {
    res.writeHead(200, { "content-type": "application/json" });
    res.end(JSON.stringify(listBooks()));
}
