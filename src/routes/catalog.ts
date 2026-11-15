import express, { Router } from "express";
import { HttpError } from "#http/errors";
import type { CatalogParser } from "#services/catalogParsePool";
import { currentRequestContext } from "../context/requestContext.ts";

// POST /api/catalog/validate: a supplier's full catalog (text/csv) is checked on a worker thread
export function catalogRouter(parse: CatalogParser): Router {
    return Router().post("/validate", express.raw({ type: "text/csv", limit: "64mb" }), async (req, res) => {
        if (!Buffer.isBuffer(req.body) || req.body.length === 0) {
            throw new HttpError(415, "send the catalog as text/csv");
        }
        // If the client disconnects, the worker running this task is stopped
        const result = await parse(req.body, currentRequestContext()?.signal);
        if (!result.ok) {
            throw new HttpError(400, result.reason);
        }
        res.json(result);
    });
}
