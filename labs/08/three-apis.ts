import { readFileSync, readFile } from "node:fs";
import { readFile as readFileAsync } from "node:fs/promises";

const path = new URL("../../src/db/books.json", import.meta.url);

// 1) Sync: blocks the event loop until the read finishes; fine at startup
const sync = readFileSync(path, "utf8");
console.log(`sync:     ${sync.length} chars`);

// 2) Callback: error-first, the oldest API
readFile(path, "utf8", (err, text) => console.log(`callback: ${err ? err.code : text.length + " chars"}`));

// 3) Promise: what we use in request handlers
const text = await readFileAsync(path, "utf8");
console.log(`promise:  ${text.length} chars`);
