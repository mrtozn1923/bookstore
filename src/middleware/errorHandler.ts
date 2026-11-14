import type { ErrorRequestHandler, RequestHandler } from "express";
import { HttpError } from "#http/errors";
import { RequestValidationError } from "#http/validation";
import { sendProblem } from "#http/problem";
import { BookNotFoundError, DomainError, MixedCurrencyError, OrderNotFoundError, OutOfStockError, UnknownBookError } from "#domain/errors";
import { currentRequestContext } from "../context/requestContext.ts";
import { log } from "../diagnostics/log.ts";

// Domain error -> HTTP status, in one place
const DOMAIN_STATUS = new Map<Function, number>([
    [BookNotFoundError, 404],
    [OrderNotFoundError, 404],
    [UnknownBookError, 422],
    [MixedCurrencyError, 422],
    [OutOfStockError, 409],
]);

// Registered after every route: nothing matched
export const notFound: RequestHandler = (req, res, next) => {
    next(new HttpError(404, `no route for ${req.method} ${req.path}`));
};

// Four parameters: that is how Express recognises an error handler
export const errorHandler: ErrorRequestHandler = (err, req, res, next) => {
    const context = currentRequestContext();
    if (context?.signal.aborted) {
        log("warn", "client went away", { method: req.method, path: req.path });
        return;
    }
    if (res.headersSent) {
        // Too late for a status code; Express's default handler closes the connection
        log("error", "error after the response started", { error: (err as Error).message });
        return next(err);
    }
    const common = { instance: req.originalUrl, traceId: context?.requestId };

    if (err instanceof RequestValidationError) {
        return sendProblem(res, 400, { title: err.message, ...common, errors: err.errors });
    }
    if (err instanceof DomainError) {
        const status = DOMAIN_STATUS.get(err.constructor) ?? 422;
        return sendProblem(res, status, { detail: err.message, ...common });
    }
    if (err instanceof HttpError) {
        return sendProblem(res, err.status, { detail: err.message, ...common });
    }
    // body-parser errors carry a status and a type; their messages are not shown to the client
    if (err?.type === "entity.parse.failed") {
        return sendProblem(res, 400, { detail: "request body is not valid JSON", ...common });
    }
    if (err?.type === "entity.too.large") {
        // The rest of the body is still on its way: close instead of reading it all
        res.setHeader("connection", "close");
        return sendProblem(res, 413, { detail: `request body is larger than ${err.limit} bytes`, ...common });
    }
    // Anything else is a bug: full details in the log, none in the response
    log("error", "unhandled error", { error: (err as Error).stack });
    sendProblem(res, 500, common);
};
