// 4 slow pbkdf2 calls fill the thread pool; what happens to fs, dns and the network?
import { pbkdf2 } from "node:crypto";
import { readFile } from "node:fs/promises";
import { lookup } from "node:dns/promises";
import { createServer } from "node:http";

const server = createServer((req, res) => res.end("ok"));
await new Promise<void>((resolve) => server.listen(0, "127.0.0.1", resolve));
const { port } = server.address() as { port: number };

const started = performance.now();
const at = (label: string) => console.log(`${label.padEnd(26)} ${Math.round(performance.now() - started)} ms`);

for (let i = 1; i <= 4; i++) {
    pbkdf2("correct horse", "bookstore-salt", 3_000_000, 32, "sha256", () => at(`pbkdf2 #${i}`));
}

await Promise.all([
    readFile(import.meta.filename).then(() => at("fs.readFile (thread pool)")),
    lookup("localhost").then(() => at("dns.lookup (thread pool)")),
    fetch(`http://127.0.0.1:${port}/`).then((r) => r.text()).then(() => at("HTTP request (OS async)")),
]);
server.close();
