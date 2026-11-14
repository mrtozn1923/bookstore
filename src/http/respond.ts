import type { ServerResponse } from "node:http";

export function sendJson(res: ServerResponse, status: number, body: unknown, headers: Record<string, string> = {}): void {
    const json = JSON.stringify(body);
    res.writeHead(status, {
        "content-type": "application/json; charset=utf-8",
        "content-length": Buffer.byteLength(json),
        ...headers,
    });
    res.end(json);
}
