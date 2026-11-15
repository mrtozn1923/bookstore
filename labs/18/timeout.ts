import http from "node:http";
import { setTimeout as delay } from "node:timers/promises";

// The carrier needs 2 seconds for every answer
const carrier = http.createServer(async (req, res) => {
    await delay(2_000);
    res.writeHead(200, { "content-type": "application/json" }).end("{}");
});
carrier.listen(52181, "127.0.0.1");
const url = "http://127.0.0.1:52181/v1/tracking/BK1001TR";

async function attempt(label: string, signal: AbortSignal): Promise<void> {
    const started = performance.now();
    try {
        await fetch(url, { signal });
        console.log(`${label}: answered`);
    } catch (err) {
        const e = err as Error;
        console.log(`${label}: ${e.name} after ${Math.round(performance.now() - started)} ms (${e.message})`);
    }
}

// 1) Our own deadline: 500 ms per attempt
await attempt("timeout", AbortSignal.timeout(500));

// 2) The client of our API left after 100 ms; the deadline is still 500 ms
const requestAborted = new AbortController();
setTimeout(() => requestAborted.abort(), 100);
await attempt("any", AbortSignal.any([requestAborted.signal, AbortSignal.timeout(500)]));

carrier.closeAllConnections();
carrier.close();
