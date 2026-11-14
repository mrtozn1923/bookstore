import { gzipSync } from "node:zlib";
import { generateOrderRows, toCsvLine } from "#services/orderExport";
import type { Book } from "#domain/book";
import books from "#db/books.json" with { type: "json" };

// The "simple" way: build the whole CSV in memory, then compress it in one go
const rows = Number(process.argv[2] ?? 5_300_000);
const started = performance.now();
let peakRss = 0;
const lines: string[] = [];
for (const row of generateOrderRows(rows, (books as Book[]).slice(0, 4))) {
    lines.push(toCsvLine(row));
    if (lines.length % 100_000 === 0) {
        peakRss = Math.max(peakRss, process.memoryUsage().rss);
    }
}
try {
    const csv = lines.join("");
    const gzipped = gzipSync(csv);
    peakRss = Math.max(peakRss, process.memoryUsage().rss);
    console.log(`${rows} rows: csv ${(csv.length / 1e6).toFixed(0)} MB, gzip ${(gzipped.length / 1e6).toFixed(1)} MB, peak rss ${(peakRss / 1024 / 1024).toFixed(0)} MB, ${Math.round(performance.now() - started)} ms before the first byte`);
} catch (err) {
    console.log(`${rows} rows: ${(err as Error).name}: ${(err as Error).message} after ${Math.round(performance.now() - started)} ms, peak rss ${(peakRss / 1024 / 1024).toFixed(0)} MB`);
}
