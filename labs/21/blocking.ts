import { readFile } from "node:fs/promises";
import { monitorEventLoopDelay } from "node:perf_hooks";
import { setTimeout as delay } from "node:timers/promises";
import { parseCatalogCsv } from "#services/catalogImport";
import { createCatalogParsePool } from "#services/catalogParsePool";

// Four suppliers upload their catalog at the same time; meanwhile /health should still answer every 10 ms
const file = await readFile("labs/21/out/catalog-500k.csv");
const pool = createCatalogParsePool({ maxThreads: 4 });
// Warm up both paths once so that start-up costs do not distort the numbers
parseCatalogCsv(file.toString("utf8"));
await pool.parse(Buffer.from(file));

async function measure(label: string, work: () => Promise<unknown>): Promise<void> {
    const histogram = monitorEventLoopDelay({ resolution: 1 });
    histogram.enable();
    // The histogram needs a first tick before it can measure the gap to the next one
    await delay(20);
    const started = performance.now();
    await work();
    const elapsed = performance.now() - started;
    // One more turn of the loop, so that the histogram's timer can record a stall that just ended
    await delay(20);
    histogram.disable();
    console.log(`${label}: 4 catalogs in ${Math.round(elapsed)} ms, longest event loop stall ${Math.round(histogram.max / 1e6)} ms`);
}

await measure("main thread", async () => {
    for (let i = 0; i < 4; i++) {
        parseCatalogCsv(file.toString("utf8"));
    }
});
await measure("worker pool", () => Promise.all(Array.from({ length: 4 }, () => pool.parse(Buffer.from(file)))));
await pool.close();
