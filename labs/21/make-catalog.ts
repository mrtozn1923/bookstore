import { createWriteStream } from "node:fs";
import { mkdir } from "node:fs/promises";
import { finished } from "node:stream/promises";

// A supplier's full catalog: N rows in the format of post 08, every 1000th row with a bad price
const rows = Number(process.argv[2] ?? 500_000);
await mkdir("labs/21/out", { recursive: true });
const out = createWriteStream(`labs/21/out/catalog-${rows / 1000}k.csv`);
const titles = ["Dune", "Emma", "The Hobbit", "Middlemarch", "Persuasion", "Ulysses"];

function isbn13(n: number): string {
    const body = `978${String(n).padStart(9, "0")}`;
    const sum = [...body].reduce((acc, ch, i) => acc + Number(ch) * (i % 2 === 0 ? 1 : 3), 0);
    return body + ((10 - (sum % 10)) % 10);
}

out.write("﻿isbn;title;author;price;currency\n");
for (let i = 0; i < rows; i++) {
    const price = i % 1000 === 999 ? "n/a" : `${100 + (i % 400)},${String(i % 100).padStart(2, "0")}`;
    if (!out.write(`${isbn13(i)};${titles[i % titles.length]} ${i};Ada Lovelace;${price};TRY\n`)) {
        await new Promise((resolve) => out.once("drain", resolve));
    }
}
out.end();
await finished(out);
console.log(`labs/21/out/catalog-${rows / 1000}k.csv: ${rows} rows`);
