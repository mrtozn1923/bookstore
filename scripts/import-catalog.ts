// Usage: node scripts/import-catalog.ts <catalog.csv> [out.json]
import path from "node:path";
import { mkdir } from "node:fs/promises";
import { readCatalogCsv } from "#services/catalogImport";
import { writeFileAtomic } from "#storage/atomicWrite";

const [csvPath, outPath = "data/catalog.json"] = process.argv.slice(2);
if (!csvPath) {
    console.error("usage: node scripts/import-catalog.ts <catalog.csv> [out.json]");
    process.exit(2);
}

const { books, errors } = await readCatalogCsv(csvPath);
for (const error of errors) {
    console.error(`skipped ${error}`);
}
await mkdir(path.dirname(outPath), { recursive: true });
await writeFileAtomic(outPath, JSON.stringify(books, null, 2) + "\n");
console.log(`imported ${books.length} books into ${outPath}, ${errors.length} rows skipped`);
process.exitCode = errors.length > 0 ? 1 : 0;
