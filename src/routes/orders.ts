import type { IncomingMessage, ServerResponse } from "node:http";
import { createGzip } from "node:zlib";
import { pipeline } from "node:stream/promises";
import { orderRowStream, csvTransform } from "#services/orderExport";
import { orderService, UnknownBookError, type OrderLine } from "#services/orderService";
import { readJsonBody } from "#http/body";
import { HttpError } from "#http/errors";
import { sendJson } from "#http/respond";
import type { Params } from "#http/router";
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
export async function handlePlaceOrder(req: IncomingMessage, res: ServerResponse): Promise<void> {
    const { customerId, lines } = parsePlaceOrder(await readJsonBody(req));
    try {
        const order = orderService.placeOrder(customerId, lines);
        sendJson(res, 201, order, { location: `/api/orders/${order.id}` });
    } catch (err) {
        if (err instanceof UnknownBookError) {
            throw new HttpError(422, err.message);
        }
        throw err;
    }
}

// GET /api/orders/:id
export function handleGetOrder(req: IncomingMessage, res: ServerResponse, { params }: { params: Params }): void {
    const order = orderService.findOrder(params.id);
    if (!order) {
        throw new HttpError(404, `order ${params.id} not found`);
    }
    sendJson(res, 200, order);
}
