import http from "node:http";
import { Agent } from "undici";

// 1 MB answer: bigger than what undici buffers for a body nobody reads
const body = Buffer.alloc(1024 * 1024, "x");
let sockets = 0;
const carrier = http.createServer((req, res) => res.end(body));
carrier.on("connection", () => sockets++);
carrier.listen(52182, "127.0.0.1");
const url = "http://127.0.0.1:52182/v1/labels/BK1001TR";

// 20 sequential calls through the global dispatcher: how many TCP connections?
for (let i = 0; i < 20; i++) {
    const response = await fetch(url);
    await response.arrayBuffer();
}
console.log(`sequential, body read: 20 requests, ${sockets} connection(s)`);

// 20 parallel calls, at most 5 connections to this origin
sockets = 0;
const limited = new Agent({ connections: 5 });
await Promise.all(
    Array.from({ length: 20 }, async () => {
        const response = await fetch(url, { dispatcher: limited } as RequestInit);
        await response.arrayBuffer();
    }),
);
console.log(`parallel, connections: 5: 20 requests, ${sockets} connection(s)`);

// One connection, and the first response's body is never read
const single = new Agent({ connections: 1 });
const first = await fetch(url, { dispatcher: single } as RequestInit);
console.log("first:", first.status, "(body not read)");
const started = performance.now();
try {
    const second = await fetch(url, { dispatcher: single, signal: AbortSignal.timeout(1_000) } as RequestInit);
    console.log("second:", second.status);
} catch (err) {
    console.log(`second: ${(err as Error).name} after ${Math.round(performance.now() - started)} ms`);
}

// Cancelling the first body gives the connection back
await first.body?.cancel();
const third = await fetch(url, { dispatcher: single, signal: AbortSignal.timeout(1_000) } as RequestInit);
console.log("after cancel():", third.status);
await third.body?.cancel();

await Promise.all([limited.close(), single.destroy()]);
carrier.closeAllConnections();
carrier.close();
