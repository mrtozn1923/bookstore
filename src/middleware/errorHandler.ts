import type { ErrorRequestHandler, RequestHandler } from "express";
import { HttpError } from "#http/errors";
import { currentRequestContext } from "../context/requestContext.ts";
import { log } from "../diagnostics/log.ts";

// Registered after every route: nothing matched
export const notFound: RequestHandler = (req, res, next) => {
    next(new HttpError(404, `no route for ${req.method} ${req.path}`));
};

// Four parameters: that is how Express recognises an error handler
export const errorHandler: ErrorRequestHandler = (err, req, res, next) => {
    if (currentRequestContext()?.signal.aborted) {
        log("warn", "client went away", { method: req.method, path: req.path });
        return;
    }
    if (res.headersSent) {
        // Too late for a status code; Express's default handler closes the connection
        log("error", "error after the response started", { error: (err as Error).message });
        return next(err);
    }
    // HttpError from our code, or errors from body-parser that carry a 4xx status (400, 413, 415)
    const status: number = err instanceof HttpError ? err.status : Number.isInteger(err?.status) && err.status >= 400 && err.status < 500 ? err.status : 500;
    if (status === 500) {
        log("error", "unhandled error", { error: (err as Error).stack });
    }
    if (status === 413) {
        // The rest of the body is still on its way: close instead of reading it all
        res.setHeader("connection", "close");
    }
    res.status(status).json({ error: status === 500 ? "internal server error" : (err as Error).message });
};
