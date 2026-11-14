import { Router } from "express";
import { takeEventLoopDelay } from "../diagnostics/eventLoopMonitor.ts";

export const diagnosticsRouter = Router();

// GET /diagnostics/event-loop: event loop delay since the previous call
diagnosticsRouter.get("/event-loop", (req, res) => {
    res.json(takeEventLoopDelay());
});
