import { createServer } from "node:http";
import { randomUUID } from "node:crypto";
import { handleHealth } from "#routes/health";
import { handleListBooks } from "#routes/books";
import { handlePriceReport } from "#routes/reports";
import { handleEventLoopDelay } from "#routes/diagnostics";
import { orderService } from "#services/orderService";
import { registerOrderNotifications } from "#services/notificationService";
import { runWithRequestContext } from "./context/requestContext.ts";
import { log } from "./diagnostics/log.ts";

const port: number = Number(process.env.PORT ?? 3000);
// Loopback only unless HOST says otherwise (details in the node:http post)
const host: string = process.env.HOST ?? "127.0.0.1";

registerOrderNotifications(orderService);

// Accept the caller's correlation id if it looks sane, otherwise create one
function requestIdFrom(header: string | string[] | undefined): string {
    return typeof header === "string" && /^[\w.-]{1,64}$/.test(header) ? header : randomUUID();
}

const server = createServer((req, res) => {
    const url = new URL(req.url ?? "/", "http://localhost");
    const requestId = requestIdFrom(req.headers["x-request-id"]);
    res.setHeader("x-request-id", requestId);

    runWithRequestContext({ requestId, method: req.method ?? "GET", path: url.pathname }, async () => {
        const started = performance.now();
        res.on("finish", () => {
            log("info", "request finished", { method: req.method, path: url.pathname, status: res.statusCode, durationMs: Math.round(performance.now() - started) });
        });

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
});

server.listen(port, host, () => {
    console.log(`Bookstore API listening on http://${host}:${port} (${process.env.NODE_ENV ?? "development"})`);
});
