// Minimal static server for a built app: node scripts/serve.mjs <dir> <port>
import { createServer } from "node:http";
import { readFile } from "node:fs/promises";
import { extname, join, normalize } from "node:path";

const [root, port = "52862"] = process.argv.slice(2);
const types = { ".html": "text/html", ".js": "text/javascript", ".css": "text/css", ".ico": "image/x-icon", ".svg": "image/svg+xml" };
createServer(async (req, res) => {
    const path = normalize(decodeURIComponent(new URL(req.url, "http://x").pathname)).replace(/^(\.\.[/\\])+/, "");
    for (const file of [join(root, path), join(root, "index.html")]) {
        try {
            const body = await readFile(file);
            res.writeHead(200, { "content-type": types[extname(file)] ?? "application/octet-stream" });
            return res.end(body);
        } catch { /* try the next candidate */ }
    }
    res.writeHead(404).end();
}).listen(Number(port), "127.0.0.1", () => console.log(`serving ${root} on http://127.0.0.1:${port}`));
