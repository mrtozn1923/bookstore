// The anti-pattern's worker: started for a single task and thrown away
import { parentPort, workerData } from "node:worker_threads";
import { parseCatalogCsv } from "#services/catalogImport";

parentPort!.postMessage(parseCatalogCsv(workerData as string).books.length);
