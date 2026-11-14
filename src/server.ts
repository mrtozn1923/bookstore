import { createServer } from "node:http";
import { handleHealth } from "#routes/health";
import { handleListBooks } from "#routes/books";
import { handlePriceReport } from "#routes/reports";
import { handleEventLoopDelay } from "#routes/diagnostics";

const port: number = Number(process.env.PORT ?? 3000);

const server = createServer(async (req, res) => {
    const url = new URL(req.url ?? "/", "http://localhost");
    if (req.method === "GET" && url.pathname === "/health") {
        return handleHealth(req, res);
    }
    if (req.method === "GET" && url.pathname === "/api/books") {
        return handleListBooks(req, res);
    }
    if (req.method === "GET" && url.pathname === "/api/reports/prices") {
        return handlePriceReport(req, res, url);
    }
    if (req.method === "GET" && url.pathname === "/diagnostics/event-loop") {
        return handleEventLoopDelay(req, res);
    }
    res.writeHead(404, { "content-type": "application/json" });
    res.end(JSON.stringify({ error: "not found" }));
});

server.listen(port, () => {
    console.log(`Bookstore API listening on http://localhost:${port} (${process.env.NODE_ENV ?? "development"})`);
});
