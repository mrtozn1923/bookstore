import { createServer } from "node:http";
import { AsyncLocalStorage } from "node:async_hooks";
import { setTimeout as delay } from "node:timers/promises";

// Wrong: one variable shared by every request in flight
let currentRequestId = "";
// Right: a value bound to the async call chain of each request
const requestContext = new AsyncLocalStorage<{ requestId: string }>();

function log(message: string): void {
    const fromStore = requestContext.getStore()?.requestId;
    console.log(`global=${currentRequestId} als=${fromStore} | ${message}`);
}

// The service never receives the request id as a parameter
async function loadOrder(orderId: string, dbLatencyMs: number): Promise<void> {
    log(`loading ${orderId}`);
    await delay(dbLatencyMs);
    log(`loaded ${orderId}`);
}

const server = createServer((req, res) => {
    const requestId = String(req.headers["x-request-id"]);
    currentRequestId = requestId;
    requestContext.run({ requestId }, async () => {
        const url = new URL(req.url ?? "/", "http://localhost");
        await loadOrder(url.searchParams.get("id")!, Number(url.searchParams.get("latency")));
        res.end("ok");
    });
});
await new Promise<void>((resolve) => server.listen(0, "127.0.0.1", resolve));
const { port } = server.address() as { port: number };

const call = (requestId: string, orderId: string, latency: number) =>
    fetch(`http://127.0.0.1:${port}/orders?id=${orderId}&latency=${latency}`, { headers: { "x-request-id": requestId } });
await Promise.all([call("req-A", "ORD-1001", 60), call("req-B", "ORD-1002", 20), call("req-C", "ORD-1003", 40)]);
server.close();
