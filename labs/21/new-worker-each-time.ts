import { once } from "node:events";
import { Worker } from "node:worker_threads";
import { Piscina } from "piscina";

// 50 small tasks (a 20-row catalog each): a new Worker per task vs. one pool
const csv = "isbn;title;author;price;currency\n" + "9780441172719;Dune;Frank Herbert;349,90;TRY\n".repeat(20);
const workerFile = new URL("../../src/workers/catalogParseWorker.ts", import.meta.url);

let started = performance.now();
for (let i = 0; i < 50; i++) {
    const worker = new Worker(new URL("./one-shot-worker.ts", import.meta.url), { workerData: csv });
    await once(worker, "message");
    await worker.terminate();
}
console.log(`new Worker per task: ${Math.round(performance.now() - started)} ms for 50 tasks`);

const pool = new Piscina({ filename: workerFile.href, minThreads: 1, maxThreads: 1 });
started = performance.now();
for (let i = 0; i < 50; i++) {
    await pool.run({ csv: new TextEncoder().encode(csv).buffer });
}
console.log(`one warm pool thread: ${Math.round(performance.now() - started)} ms for 50 tasks`);
await pool.close();
