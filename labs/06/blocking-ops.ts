// Two common ways to block the event loop without noticing
import { monitorEventLoopDelay } from "node:perf_hooks";
import { pbkdf2Sync } from "node:crypto";
import { setTimeout as delay } from "node:timers/promises";

const histogram = monitorEventLoopDelay({ resolution: 10 });
histogram.enable();

async function measure(label: string, work: () => void): Promise<void> {
    await delay(50);
    histogram.reset();
    await delay(50);
    const started = performance.now();
    work();
    const took = performance.now() - started;
    await delay(50);
    console.log(`${label.padEnd(34)} ${took.toFixed(0).padStart(5)} ms on the main thread, loop delay max ${(histogram.max / 1e6).toFixed(0)} ms`);
}

// 1) A big JSON payload: 300,000 order lines
const lines = Array.from({ length: 1_000_000 }, (_, i) => ({ orderId: i, isbn: "9780441172719", quantity: 2, unitPrice: 349.9, status: "Paid" }));
const payload = JSON.stringify(lines);
console.log(`payload size: ${(payload.length / 1024 / 1024).toFixed(1)} MB`);
await measure("JSON.parse(payload)", () => JSON.parse(payload));

// 2) Synchronous password hashing in a request handler
await measure("pbkdf2Sync, 600,000 iterations", () => pbkdf2Sync("correct horse", "bookstore-salt", 600_000, 32, "sha256"));

