import { createServer } from "node:http";
import { randomUUID } from "node:crypto";
import { Router } from "#http/router";
import { HttpError } from "#http/errors";
import { sendJson } from "#http/respond";
import { handleHealth } from "#routes/health";
import { handleListBooks, handleGetBook } from "#routes/books";
import { handlePlaceOrder, handleGetOrder, handleOrderExport } from "#routes/orders";
import { handlePriceReport } from "#routes/reports";
import { handleEventLoopDelay } from "#routes/diagnostics";
import { orderService } from "#services/orderService";
import { registerOrderNotifications } from "#services/notificationService";
import { runWithRequestContext } from "./context/requestContext.ts";
import { log } from "./diagnostics/log.ts";

const port: number = Number(process.env.PORT ?? 3000);
// Loopback only by default; containers set HOST=0.0.0.0
const host: string = process.env.HOST ?? "127.0.0.1";

registerOrderNotifications(orderService);

const router = new Router()
    .add("GET", "/health", (req, res) => handleHealth(req, res))
    .add("GET", "/api/books", (req, res) => handleListBooks(req, res))
    .add("GET", "/api/books/:isbn", handleGetBook)
    .add("POST", "/api/orders", (req, res) => handlePlaceOrder(req, res))
    .add("GET", "/api/orders/export", (req, res, { url }) => handleOrderExport(req, res, url))
    .add("GET", "/api/orders/:id", handleGetOrder)
    .add("GET", "/api/reports/prices", (req, res, { url }) => handlePriceReport(req, res, url))
    .add("GET", "/diagnostics/event-loop", (req, res) => handleEventLoopDelay(req, res));

// Accept the caller's correlation id if it looks sane, otherwise create one
function requestIdFrom(header: string | string[] | undefined): string {
    return typeof header === "string" && /^[\w.-]{1,64}$/.test(header) ? header : randomUUID();
}

const server = createServer(
    {
        // Stated explicitly even where they equal the defaults, so they show up in code review
        headersTimeout: 10_000,
        requestTimeout: 30_000,
        keepAliveTimeout: 5_000,
        maxHeaderSize: 16 * 1024,
    },
    (req, res) => {
        const url = new URL(req.url ?? "/", "http://localhost");
        const requestId = requestIdFrom(req.headers["x-request-id"]);
        res.setHeader("x-request-id", requestId);

        const aborted = new AbortController();
        res.on("close", () => {
            if (!res.writableFinished) {
                aborted.abort(new Error("client closed the connection"));
            }
        });

        runWithRequestContext({ requestId, method: req.method ?? "GET", path: url.pathname, signal: aborted.signal }, async () => {
            const started = performance.now();
            res.on("finish", () => {
                log("info", "request finished", { method: req.method, path: url.pathname, status: res.statusCode, durationMs: Math.round(performance.now() - started) });
            });

            try {
                const match = router.match(req.method ?? "GET", url.pathname);
                if (!match) {
                    throw new HttpError(404, `no route for ${url.pathname}`);
                }
                if ("allowed" in match) {
                    res.setHeader("allow", match.allowed.join(", "));
                    throw new HttpError(405, `${req.method} not allowed on ${url.pathname}`);
                }
                await match.handler(req, res, { url, params: match.params });
            } catch (err) {
                if (aborted.signal.aborted) {
                    log("warn", "client went away", { method: req.method, path: url.pathname, durationMs: Math.round(performance.now() - started) });
                } else if (res.headersSent) {
                    log("error", "error after the response started", { error: (err as Error).message });
                    res.destroy();
                } else if (err instanceof HttpError) {
                    // After 413 the rest of the body is still on its way: close instead of reading it all
                    sendJson(res, err.status, { error: err.message }, err.status === 413 ? { connection: "close" } : {});
                } else {
                    log("error", "unhandled error", { error: (err as Error).stack });
                    sendJson(res, 500, { error: "internal server error" });
                }
            }
        });
    },
);

server.listen(port, host, () => {
    console.log(`Bookstore API listening on http://${host}:${port} (${process.env.NODE_ENV ?? "development"})`);
});
