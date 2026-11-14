import { readFile } from "node:fs/promises";

const text = await readFile(new URL("../../data/catalog-sample.csv", import.meta.url), "utf8");
const firstColumn = text.split(/\r?\n/)[0].split(";")[0];
console.log(firstColumn, firstColumn.length, firstColumn === "isbn", firstColumn.charCodeAt(0).toString(16));
