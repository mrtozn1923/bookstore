import { createServer } from "node:http";

// One async handler, one bug: what does the client get?
const server = createServer(async (req, res) => {
    if (req.url === "/api/books/9780000000000") {
        const book = undefined as { title: string } | undefined;
        res.end(book!.title);
        return;
    }
    res.end("ok");
});

server.listen(3100, "127.0.0.1", () => console.log("listening on 3100"));
