import { Writable } from "node:stream";
import { once } from "node:events";

// A destination that is slower than the producer: one event loop turn per chunk
function slowDestination(): Writable {
    return new Writable({
        write(chunk, encoding, callback) {
            setImmediate(callback);
        },
    });
}

async function produce(respectBackpressure: boolean): Promise<void> {
    const out = slowDestination();
    let peak = 0;
    let peakHeap = 0;
    const started = performance.now();
    for (let i = 1; i <= 1_000_000; i++) {
        const ok = out.write(`ORD-${100000 + i},9780441172719,2,349.90\n`);
        peak = Math.max(peak, out.writableLength);
        if (i % 10_000 === 0) {
            peakHeap = Math.max(peakHeap, process.memoryUsage().heapUsed);
        }
        if (!ok && respectBackpressure) {
            // Wait until the destination has drained its buffer
            await once(out, "drain");
        }
    }
    out.end();
    await once(out, "finish");
    const label = respectBackpressure ? "await drain" : "ignore false";
    console.log(`${label.padEnd(13)} peak buffer ${(peak / 1024 / 1024).toFixed(2).padStart(6)} MB, peak heap ${(peakHeap / 1024 / 1024).toFixed(0).padStart(3)} MB, ${Math.round(performance.now() - started)} ms`);
}

await produce(process.argv[2] !== "ignore");
