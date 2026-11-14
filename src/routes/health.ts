import type { IncomingMessage, ServerResponse } from "node:http";

// GET /health: is the process up, and which Node version is it running on?
export function handleHealth(req: IncomingMessage, res: ServerResponse): void {
    const body = {
        status: "ok",
        service: "bookstore-api",
        node: process.version,
        uptimeSeconds: Math.round(process.uptime()),
    };
    res.writeHead(200, { "content-type": "application/json" });
    res.end(JSON.stringify(body));
}
