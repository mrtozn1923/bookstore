import { createServer } from "node:http";
import { handleHealth } from "./routes/health.js";

const port = Number(process.env.PORT ?? 3000);

const server = createServer((req, res) => {
    if (req.method === "GET" && req.url === "/health") {
        return handleHealth(req, res);
    }
    res.writeHead(404, { "content-type": "application/json" });
    res.end(JSON.stringify({ error: "not found" }));
});

server.listen(port, () => {
    console.log(`Bookstore API listening on http://localhost:${port} (${process.env.NODE_ENV ?? "development"})`);
});
