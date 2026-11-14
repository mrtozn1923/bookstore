// Compares compressed sizes and compression times of real Bookstore responses:
// gzip, Brotli quality 1 / 4 / 11 (= .NET's Fastest / Optimal / SmallestSize) and zstd level 3 / 19.
// Usage: node compress-sizes.mjs <dist-browser-folder> <api-origin>
import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';
import zlib from 'node:zlib';

const [dist, api] = process.argv.slice(2);
const js = readdirSync(dist).filter((f) => f.endsWith('.js')).sort((a, b) => statSync(join(dist, b)).size - statSync(join(dist, a)).size);
const inputs = [
  [js[0], readFileSync(join(dist, js[0]))],
  ['book page (SSR HTML)', Buffer.from(await (await fetch(`${api}/books/9781000000009`)).arrayBuffer())],
  ['/api/books (JSON)', Buffer.from(await (await fetch(`${api}/api/books`)).arrayBuffer())],
];
const { BROTLI_PARAM_QUALITY: Q, ZSTD_c_compressionLevel: Z } = zlib.constants;
const codecs = [
  ['gzip 6', (b) => zlib.gzipSync(b, { level: 6 })],
  ['br 1', (b) => zlib.brotliCompressSync(b, { params: { [Q]: 1 } })],
  ['br 4', (b) => zlib.brotliCompressSync(b, { params: { [Q]: 4 } })],
  ['br 11', (b) => zlib.brotliCompressSync(b, { params: { [Q]: 11 } })],
  ['zstd 3', (b) => zlib.zstdCompressSync(b, { params: { [Z]: 3 } })],
  ['zstd 19', (b) => zlib.zstdCompressSync(b, { params: { [Z]: 19 } })],
];
console.log(`${'response'.padEnd(24)}${'raw'.padStart(9)}${codecs.map(([n]) => n.padStart(16)).join('')}`);
for (const [name, buf] of inputs) {
  const cells = codecs.map(([, fn]) => {
    const times = [];
    let out;
    for (let i = 0; i < 5; i++) {
      const t = performance.now();
      out = fn(buf);
      times.push(performance.now() - t);
    }
    const ms = times.sort((a, b) => a - b)[2];
    return `${(out.length / 1000).toFixed(1)} kB ${ms.toFixed(1).padStart(5)}ms`.padStart(16);
  });
  console.log(`${name.padEnd(24)}${(buf.length / 1000).toFixed(1).padStart(6)} kB${cells.join('')}`);
}
