// Runs on a separate thread with its own V8 isolate and event loop
import { parentPort, workerData } from "node:worker_threads";
import { buildPriceReport } from "#services/priceReport";

const { rows } = workerData as { rows: number };
parentPort!.postMessage(buildPriceReport(rows));
