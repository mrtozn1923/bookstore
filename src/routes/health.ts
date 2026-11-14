import type { RequestHandler } from "express";

// GET /health: is the process up, and which Node version is it running on?
export const health: RequestHandler = (req, res) => {
    res.json({
        status: "ok",
        service: "bookstore-api",
        node: process.version,
        uptimeSeconds: Math.round(process.uptime()),
    });
};
