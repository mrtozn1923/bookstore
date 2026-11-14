// Small load test for one page: N requests with C in parallel, prints throughput and latency
// percentiles. Lab only: client and server run on the same machine.
// Usage: node ssr-bench.mjs <url> [--requests 400] [--concurrency 8]
import { parseArgs } from 'node:util';

const { values, positionals } = parseArgs({
  allowPositionals: true,
  options: { requests: { type: 'string', default: '400' }, concurrency: { type: 'string', default: '8' } },
});
const url = positionals[0];
const total = Number(values.requests);
const times = [];
let next = 0;
const worker = async () => {
  while (next < total) {
    next++;
    const start = performance.now();
    const res = await fetch(url, { headers: { Accept: 'text/html' } });
    await res.arrayBuffer();
    times.push(performance.now() - start);
  }
};
const started = performance.now();
await Promise.all(Array.from({ length: Number(values.concurrency) }, worker));
const seconds = (performance.now() - started) / 1000;
times.sort((a, b) => a - b);
const p = (q) => times[Math.ceil(q * times.length) - 1].toFixed(1);
console.log(`${url}  ${total} requests, ${values.concurrency} parallel`);
console.log(`${(total / seconds).toFixed(0)} req/s  p50 ${p(0.5)} ms  p95 ${p(0.95)} ms  p99 ${p(0.99)} ms`);
