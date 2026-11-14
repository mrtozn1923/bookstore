import { Readable } from "node:stream";
import type { ReadableStream as NodeReadableStream } from "node:stream/web";
import { createInterface } from "node:readline";

const base = process.env.API_URL ?? "http://127.0.0.1:3000";

// 1) Read the export line by line: fetch gives a web ReadableStream, readline wants a Node stream
const response = await fetch(`${base}/api/orders/export?rows=1000000`, { headers: { "accept-encoding": "gzip" } });
console.log(`content-encoding: ${response.headers.get("content-encoding")}`);
// fetch's body is typed with the DOM lib; Node's stream/web type is the same object at runtime
const body = response.body as NodeReadableStream<Uint8Array>;
const lines = createInterface({ input: Readable.fromWeb(body), crlfDelay: Infinity });

let count = 0;
let deliveredRevenue = 0;
for await (const line of lines) {
    if (count++ === 0) {
        continue; // header row
    }
    const fields = line.split(",");
    if (fields[8] === "Delivered") {
        deliveredRevenue += Number(fields[5]) * Number(fields[6]);
    }
}
console.log(`rows: ${count - 1}, delivered revenue: ${deliveredRevenue.toFixed(2)} TRY, heap ${(process.memoryUsage().heapUsed / 1024 / 1024).toFixed(0)} MB`);

// 2) Give up half way: the server should stop generating rows
const controller = new AbortController();
const big = await fetch(`${base}/api/orders/export?rows=5300000`, { signal: controller.signal });
let received = 0;
try {
    for await (const chunk of big.body!) {
        received += chunk.byteLength;
        if (received > 50 * 1024 * 1024) {
            controller.abort();
        }
    }
} catch (err) {
    console.log(`client stopped after ${(received / 1024 / 1024).toFixed(0)} MB: ${(err as Error).name}`);
}
