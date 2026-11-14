// ESM: import/export, import.meta and top-level await
import { readFile } from "node:fs/promises";
import { join } from "node:path";

console.log("dirname:", import.meta.dirname.split("/").pop());
console.log("filename:", import.meta.filename.split("/").pop());
console.log("typeof require:", typeof require);

// Top-level await: no async main() wrapper needed
const books = JSON.parse(await readFile(join(import.meta.dirname, "books.json"), "utf8"));
console.log("books:", books.map((b) => b.title).join(", "));
