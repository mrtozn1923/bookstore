import { Router } from "express";
import { createGzip } from "node:zlib";
import { pipeline } from "node:stream/promises";
import { orderRowStream, csvTransform } from "#services/orderExport";
import { orderService, UnknownBookError, type OrderLine } from "#services/orderService";
import { HttpError } from "#http/errors";
import { log } from "../diagnostics/log.ts";

export const ordersRouter = Router();

// Hand-written validation: exactly the part a schema library will take over
function parsePlaceOrder(body: unknown): { customerId: string; lines: OrderLine[] } {
    const errors: string[] = [];
    const input = (typeof body === "object" && body !== null ? body : {}) as Record<string, unknown>;
    if (typeof input.customerId !== "string" || input.customerId.trim() === "") {
        errors.push("customerId must be a non-empty string");
    }
    if (!Array.isArray(input.lines) || input.lines.length === 0 || input.lines.length > 50) {
        errors.push("lines must be an array with 1 to 50 items");
    } else {
        input.lines.forEach((line: { isbn?: unknown; quantity?: unknown }, i: number) => {
            if (typeof line?.isbn !== "string") {
                errors.push(`lines[${i}].isbn must be a string`);
            }
            if (!Number.isInteger(line?.quantity) || (line.quantity as number) < 1 || (line.quantity as number) > 20) {
                errors.push(`lines[${i}].quantity must be an integer between 1 and 20`);
            }
        });
    }
    if (errors.length > 0) {
        throw new HttpError(400, errors.join("; "));
    }
    return { customerId: input.customerId as string, lines: input.lines as OrderLine[] };
}

// POST /api/orders
ordersRouter.post("/", (req, res) => {
    if (!req.is("application/json")) {
        throw new HttpError(415, "content-type must be application/json");
    }
    const { customerId, lines } = parsePlaceOrder(req.body);
    try {
        const order = orderService.placeOrder(customerId, lines);
        res.status(201).location(`/api/orders/${order.id}`).json(order);
    } catch (err) {
        if (err instanceof UnknownBookError) {
            throw new HttpError(422, err.message);
        }
        throw err;
    }
});

// GET /api/orders/export?rows=N: CSV, gzip-compressed when the client accepts it
ordersRouter.get("/export", async (req, res) => {
    const rows = Number(req.query.rows ?? 100_000);
    const gzip = req.acceptsEncodings("gzip", "identity") === "gzip";
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
});

// GET /api/orders/:id (after /export, so "export" is never taken for an id)
ordersRouter.get("/:id", (req, res) => {
    const order = orderService.findOrder(req.params.id);
    if (!order) {
        throw new HttpError(404, `order ${req.params.id} not found`);
    }
    res.json(order);
});
