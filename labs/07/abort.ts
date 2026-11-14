import { createServer } from "node:http";
import { setTimeout as delay } from "node:timers/promises";

// A slow stock service: answers after 2 seconds
const stock = createServer(async (req, res) => {
    await delay(2000);
    res.end(JSON.stringify({ isbn: "9780441172719", available: 12 }));
});
await new Promise<void>((resolve) => stock.listen(0, "127.0.0.1", resolve));
const { port } = stock.address() as { port: number };
const url = `http://127.0.0.1:${port}/stock/9780441172719`;

// 1) Timeout only: CancellationTokenSource.CancelAfter counterpart
let started = performance.now();
try {
    await fetch(url, { signal: AbortSignal.timeout(300) });
} catch (err) {
    const e = err as Error;
    console.log(`timeout:  ${e.name} after ${Math.round(performance.now() - started)} ms (${e.message})`);
}

// 2) Linked signals: the client went away OR the timeout expired, whichever comes first
const clientGone = new AbortController();
setTimeout(() => clientGone.abort(new Error("client closed the connection")), 100);
started = performance.now();
try {
    await fetch(url, { signal: AbortSignal.any([clientGone.signal, AbortSignal.timeout(300)]) });
} catch (err) {
    const e = err as Error;
    console.log(`any():    ${e.name} after ${Math.round(performance.now() - started)} ms (${e.message})`);
}

// 3) Promise-based timers accept a signal too
const ac = new AbortController();
setTimeout(() => ac.abort(), 50);
try {
    await delay(1000, undefined, { signal: ac.signal });
} catch (err) {
    console.log(`delay():  ${(err as Error).name}`);
}

stock.closeAllConnections();
stock.close();
