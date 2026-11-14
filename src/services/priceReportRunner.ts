import { Worker } from "node:worker_threads";
import { once } from "node:events";
import type { PriceReport } from "#services/priceReport";

const workerUrl = new URL("../workers/priceReportWorker.ts", import.meta.url);

// Offloads the report to a worker thread; the main event loop stays free
export async function buildPriceReportInWorker(rows: number): Promise<PriceReport> {
    const worker = new Worker(workerUrl, { workerData: { rows } });
    const [report] = (await once(worker, "message")) as [PriceReport];
    return report;
}
