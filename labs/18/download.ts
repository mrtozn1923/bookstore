import { createWriteStream } from "node:fs";
import { mkdir, stat, writeFile } from "node:fs/promises";
import { Readable } from "node:stream";
import { pipeline } from "node:stream/promises";
import type { ReadableStream } from "node:stream/web";

const mode = process.argv[2] ?? "stream";
const target = new URL(`./out/feed-${mode}.csv`, import.meta.url);
await mkdir(new URL("./out/", import.meta.url), { recursive: true });

let peak = 0;
const sampler = setInterval(() => (peak = Math.max(peak, process.memoryUsage.rss())), 20);
const started = performance.now();

const response = await fetch("http://127.0.0.1:52183/feeds/prices.csv", { signal: AbortSignal.timeout(60_000) });
if (!response.ok || !response.body) {
    throw new Error(`feed answered ${response.status}`);
}
if (mode === "text") {
    // The whole body as one string in memory
    await writeFile(target, await response.text());
} else {
    // fetch gives a web ReadableStream; Readable.fromWeb turns it into a Node stream for pipeline (post 09)
    await pipeline(Readable.fromWeb(response.body as ReadableStream<Uint8Array>), createWriteStream(target));
}

clearInterval(sampler);
const { size } = await stat(target);
console.log(`${mode}: ${(size / 1e6).toFixed(1)} MB in ${Math.round(performance.now() - started)} ms, peak RSS ${Math.round(peak / 1e6)} MB`);
