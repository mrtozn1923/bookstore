import type { IncomingMessage, ServerResponse } from "node:http";
import { createGzip } from "node:zlib";
import { pipeline } from "node:stream/promises";
import { orderRowStream, csvTransform } from "#services/orderExport";
import { log } from "../diagnostics/log.ts";

// GET /api/orders/export?rows=N: CSV, gzip-compressed when the client accepts it
export async function handleOrderExport(req: IncomingMessage, res: ServerResponse, url: URL): Promise<void> {
    const rows = Number(url.searchParams.get("rows") ?? 100_000);
    const gzip = /\bgzip\b/.test(req.headers["accept-encoding"] ?? "");
    const started = performance.now();

    res.writeHead(200, {
        "content-type": "text/csv; charset=utf-8",
        "content-disposition": 'attachment; filename="orders.csv"',
        ...(gzip ? { "content-encoding": "gzip" } : {}),
    });

    const stages = gzip ? [csvTransform(), createGzip()] : [csvTransform()];
    try {
        // pipeline wires backpressure through every stage and destroys all of them on error
        await pipeline(orderRowStream(rows), ...stages, res);
        log("info", "order export finished", { rows, gzip, durationMs: Math.round(performance.now() - started) });
    } catch (err) {
        // A client that disconnects mid-download ends up here as ERR_STREAM_PREMATURE_CLOSE
        log("warn", "order export aborted", { error: (err as NodeJS.ErrnoException).code ?? (err as Error).message, durationMs: Math.round(performance.now() - started) });
    }
}
