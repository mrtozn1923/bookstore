import { workerData } from "node:worker_threads";

const { shared, mode } = workerData as { shared: Int32Array; mode: string };
for (let i = 0; i < 1_000_000; i++) {
    if (mode === "atomics") {
        // Read, add and write as one indivisible step
        Atomics.add(shared, 0, 1);
    } else {
        // Read, add, write: another thread can write in between
        shared[0]++;
    }
}
