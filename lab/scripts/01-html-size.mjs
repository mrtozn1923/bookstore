// How much does the longer class list cost once a page repeats the card?
import { readFileSync } from "node:fs";
import { gzipSync } from "node:zlib";

const books = ["Dune", "Emma", "The Hobbit", "Middlemarch", "Persuasion", "Beloved"];
const render = (card, i) => card
    .replaceAll("Dune", `${books[i % books.length]} vol. ${i + 1}`)
    .replace("349.00", (100 + i * 7).toFixed(2));

for (const kind of ["semantic", "utility"]) {
    const card = readFileSync(`01/${kind}/book-card.html`, "utf8");
    for (const n of [1, 24]) {
        const html = Array.from({ length: n }, (_, i) => render(card, i)).join("\n");
        console.log(`${kind.padEnd(8)} x${String(n).padEnd(2)}: ${String(html.length).padStart(6)} B, gzip ${String(gzipSync(html).length).padStart(5)} B`);
    }
}
