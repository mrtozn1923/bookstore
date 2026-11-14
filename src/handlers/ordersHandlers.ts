import type { RequestHandler } from "express";
import { createGzip } from "node:zlib";
import { pipeline } from "node:stream/promises";
import { z } from "zod";
import type { OrderService } from "#services/orderService";
import type { BookService } from "#services/bookService";
import { orderRowStream, csvTransform } from "#services/orderExport";
import { placeOrderBody } from "#schemas/orders";
import { parseRequest } from "#http/validation";
import { HttpError } from "#http/errors";
import { toOrderDto } from "#handlers/dto";
import { log } from "../diagnostics/log.ts";

// POST /api/orders: validate, call the service, map the result; no business rules here
export function placeOrder(orders: OrderService): RequestHandler {
    return async (req, res) => {
        if (!req.is("application/json")) {
            throw new HttpError(415, "content-type must be application/json");
        }
        const body = parseRequest(placeOrderBody, req.body);
        const order = await orders.placeOrder(body.customerId, body.lines);
        res.status(201).location(`/api/orders/${order.id}`).json(toOrderDto(order));
    };
}

// GET /api/orders/:id
export function getOrder(orders: OrderService): RequestHandler {
    return async (req, res) => {
        res.json(toOrderDto(await orders.get(req.params.id as string)));
    };
}

const exportQuery = z.object({ rows: z.coerce.number().int().min(1).max(10_000_000).default(100_000) });

// GET /api/orders/export?rows=N: CSV, gzip-compressed when the client accepts it
export function exportOrders(books: BookService): RequestHandler {
    return async (req, res) => {
        const { rows } = parseRequest(exportQuery, req.query);
        const catalog = (await books.list({ page: 1, pageSize: 50, sort: "title" })).items;
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
            await pipeline(orderRowStream(rows, catalog), ...stages, res);
            log("info", "order export finished", { rows, gzip, durationMs: Math.round(performance.now() - started) });
        } catch (err) {
            // A client that disconnects mid-download ends up here as ERR_STREAM_PREMATURE_CLOSE
            log("warn", "order export aborted", { error: (err as NodeJS.ErrnoException).code ?? (err as Error).message, durationMs: Math.round(performance.now() - started) });
        }
    };
}
