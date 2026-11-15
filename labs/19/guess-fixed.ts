import { createWriteStream } from "node:fs";
import { mkdirSync } from "node:fs";
import { finished } from "node:stream/promises";

// The same export, but the stream is ended and awaited, and the process exits on its own
mkdirSync("labs/19/out", { recursive: true });
const out = createWriteStream("labs/19/out/orders.csv");
for (let i = 1; i <= 100_000; i++) {
    out.write(`${1000 + i};9780441172719;1;349.90\n`);
}
out.end();
await finished(out);
console.log("export written");
process.exitCode = 0;
