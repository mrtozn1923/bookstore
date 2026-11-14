import { createServer } from "node:http";
import { setTimeout as delay } from "node:timers/promises";
const server = createServer({ requestTimeout: 1000, connectionsCheckingInterval: 200 }, async (req, res) => {
    res.writeHead(200);
    for (let i = 0; i < 6; i++) { res.write(`chunk ${i}\n`); await delay(500); }
    res.end("done\n");
});
await new Promise<void>((r) => server.listen(0, "127.0.0.1", r));
const { port } = server.address() as { port: number };
const t = performance.now();
const body = await (await fetch(`http://127.0.0.1:${port}/`)).text();
console.log(body.trim().split("\n").length, "lines in", Math.round(performance.now() - t), "ms");
server.close();
