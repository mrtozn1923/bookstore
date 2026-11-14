import type { IncomingMessage, ServerResponse } from "node:http";
import { takeEventLoopDelay } from "../diagnostics/eventLoopMonitor.ts";

// GET /diagnostics/event-loop: event loop delay since the previous call
export function handleEventLoopDelay(req: IncomingMessage, res: ServerResponse): void {
    res.writeHead(200, { "content-type": "application/json" });
    res.end(JSON.stringify(takeEventLoopDelay()));
}
