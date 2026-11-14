import type { RequestHandler } from "express";
import { randomUUID } from "node:crypto";
import { runWithRequestContext } from "../context/requestContext.ts";
import { log } from "../diagnostics/log.ts";

// Accept the caller's correlation id if it looks sane, otherwise create one
function requestIdFrom(header: string | string[] | undefined): string {
    return typeof header === "string" && /^[\w.-]{1,64}$/.test(header) ? header : randomUUID();
}

// First middleware: request id, abort signal and AsyncLocalStorage context for everything after it
export const requestContext: RequestHandler = (req, res, next) => {
    const requestId = requestIdFrom(req.headers["x-request-id"]);
    res.setHeader("x-request-id", requestId);

    const aborted = new AbortController();
    res.on("close", () => {
        if (!res.writableFinished) {
            aborted.abort(new Error("client closed the connection"));
        }
    });

    // Captured now: routers rewrite req.url while they handle the request
    const path = req.path;
    const started = performance.now();
    res.on("finish", () => {
        log("info", "request finished", { method: req.method, path, status: res.statusCode, durationMs: Math.round(performance.now() - started) });
    });

    runWithRequestContext({ requestId, method: req.method, path, signal: aborted.signal }, next);
};
