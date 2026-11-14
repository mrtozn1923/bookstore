import type { RequestHandler } from "express";
import type { ReadinessCheck } from "../lifecycle/readiness.ts";

// GET /health (liveness): is the process up? Never touches dependencies, so a slow database cannot get the process restarted
export const health: RequestHandler = (req, res) => {
    res.json({
        status: "ok",
        service: "bookstore-api",
        node: process.version,
        uptimeSeconds: Math.round(process.uptime()),
    });
};

// GET /health/ready (readiness): should traffic be sent here? 503 takes the instance out of the load balancer
export function readiness(check: ReadinessCheck): RequestHandler {
    return async (req, res) => {
        const report = await check();
        res.status(report.status === "ready" ? 200 : 503).json(report);
    };
}
