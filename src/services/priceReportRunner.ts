import { Worker } from "node:worker_threads";
import { once } from "node:events";
import type { PriceReport } from "#services/priceReport";

const workerUrl = new URL("../workers/priceReportWorker.ts", import.meta.url);

// Offloads the report to a worker thread; the main event loop stays free
export async function buildPriceReportInWorker(rows: number, signal?: AbortSignal): Promise<PriceReport> {
    const worker = new Worker(workerUrl, { workerData: { rows } });
    try {
        const [report] = (await once(worker, "message", { signal })) as [PriceReport];
        return report;
    } finally {
        // Stops the thread if the caller gave up; harmless after a normal finish
        await worker.terminate();
    }
}
