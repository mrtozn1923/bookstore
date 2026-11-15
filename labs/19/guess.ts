import { createWriteStream } from "node:fs";
import { mkdirSync } from "node:fs";

// An export script: 100,000 order lines into a CSV file, then exit
mkdirSync("labs/19/out", { recursive: true });
const out = createWriteStream("labs/19/out/orders.csv");
for (let i = 1; i <= 100_000; i++) {
    out.write(`${1000 + i};9780441172719;1;349.90\n`);
}
console.log("export written");
process.exit(0);
