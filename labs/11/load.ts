// Minimal load generator: N concurrent loops of GET for a fixed duration
const url = process.argv[2] ?? "http://127.0.0.1:3000/api/books";
const concurrency = Number(process.argv[3] ?? 50);
const seconds = Number(process.argv[4] ?? 10);

const latencies: number[] = [];
const deadline = performance.now() + seconds * 1000;
let errors = 0;

async function loop(): Promise<void> {
    while (performance.now() < deadline) {
        const started = performance.now();
        try {
            const res = await fetch(url);
            await res.arrayBuffer();
            latencies.push(performance.now() - started);
        } catch {
            errors++;
        }
    }
}

await Promise.all(Array.from({ length: concurrency }, loop));
latencies.sort((a, b) => a - b);
const p = (q: number) => latencies[Math.floor(latencies.length * q)].toFixed(1);
console.log(`${(latencies.length / seconds).toFixed(0)} req/s, p50 ${p(0.5)} ms, p99 ${p(0.99)} ms, errors ${errors}`);
