import { readFile } from "node:fs/promises";
import { once } from "node:events";
import { Worker } from "node:worker_threads";

// The 26 MB catalog from labs/21/make-catalog.ts, sent to a worker twice
const worker = new Worker(new URL("./echo-worker.ts", import.meta.url));

for (const mode of ["copy", "transfer"] as const) {
    const bytes = (await readFile("labs/21/out/catalog-500k.csv")).buffer as ArrayBuffer;
    const started = performance.now();
    worker.postMessage(bytes, mode === "transfer" ? [bytes] : []);
    const [received] = await once(worker, "message");
    console.log(`${mode.padEnd(8)}: worker got ${received} bytes in ${(performance.now() - started).toFixed(1)} ms, main thread still has ${bytes.byteLength} bytes`);
}
await worker.terminate();
