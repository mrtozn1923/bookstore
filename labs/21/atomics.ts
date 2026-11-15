import { once } from "node:events";
import { Worker } from "node:worker_threads";

// Four workers count imported rows into one shared counter, 1,000,000 times each
for (const mode of ["plain", "atomics"]) {
    const shared = new Int32Array(new SharedArrayBuffer(4));
    const workers = Array.from({ length: 4 }, () => new Worker(new URL("./counter-worker.ts", import.meta.url), { workerData: { shared, mode } }));
    await Promise.all(workers.map((worker) => once(worker, "exit")));
    console.log(`${mode.padEnd(7)}: ${shared[0].toLocaleString("en-US")} (expected 4,000,000)`);
}
