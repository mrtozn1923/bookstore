import { writeFile, readFile, mkdir } from "node:fs/promises";
import { writeFileAtomic } from "#storage/atomicWrite";

const target = "labs/08/out/catalog.json";
const catalog = (n: number) => JSON.stringify(Array.from({ length: 200_000 }, (_, i) => ({ isbn: `978${String(i).padStart(10, "0")}`, version: n })));

// A reader polls the file while a writer replaces it 20 times
async function run(label: string, write: (data: string) => Promise<void>): Promise<void> {
    await writeFileAtomic(target, catalog(0));
    let writing = true;
    let reads = 0;
    let broken = 0;
    const reader = (async () => {
        while (writing) {
            const text = await readFile(target, "utf8");
            reads++;
            try {
                JSON.parse(text);
            } catch {
                broken++;
            }
        }
    })();
    for (let n = 1; n <= 20; n++) {
        await write(catalog(n));
    }
    writing = false;
    await reader;
    console.log(`${label.padEnd(16)} reads: ${reads}, half-written: ${broken}`);
}

await mkdir("labs/08/out", { recursive: true });
await run("writeFile", (data) => writeFile(target, data));
await run("writeFileAtomic", (data) => writeFileAtomic(target, data));
