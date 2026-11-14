import { readFile, writeFile, mkdir } from "node:fs/promises";

const file = "labs/08/out/stock.json";
await mkdir("labs/08/out", { recursive: true });
await writeFile(file, JSON.stringify({ isbn: "9780441172719", reserved: 0 }));

// Read-modify-write without coordination: 50 concurrent reservations
async function reserveOne(): Promise<void> {
    const stock = JSON.parse(await readFile(file, "utf8"));
    stock.reserved += 1;
    await writeFile(file, JSON.stringify(stock));
}

await Promise.all(Array.from({ length: 50 }, reserveOne));
const final = JSON.parse(await readFile(file, "utf8"));
console.log(`expected 50, file says ${final.reserved}`);
