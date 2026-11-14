// Serves ../web/dist/bookstore-web/browser and prints computed styles.
// usage: node scripts/web-probe.mjs "<selector>" prop1,prop2 [light|dark] [width]
import { createServer } from "node:http";
import { readFile } from "node:fs/promises";
import { extname, join, resolve } from "node:path";
import { chromium } from "playwright";

const root = resolve(process.env.WEB_DIST ?? "../web/dist/bookstore-web/browser");
const types = { ".html": "text/html", ".js": "text/javascript", ".css": "text/css", ".ico": "image/x-icon" };
const server = createServer(async (req, res) => {
    const path = req.url === "/" ? "/index.html" : req.url.split("?")[0];
    try {
        const body = await readFile(join(root, path));
        res.writeHead(200, { "content-type": types[extname(path)] ?? "application/octet-stream" }).end(body);
    } catch {
        res.writeHead(404).end();
    }
}).listen(0);

const [selector, props, scheme = "light", width = "1280"] = process.argv.slice(2);
const browser = await chromium.launch();
const page = await browser.newPage({ colorScheme: scheme, viewport: { width: Number(width), height: 800 } });
await page.goto(`http://127.0.0.1:${server.address().port}/`);
await page.waitForSelector(selector);
const rows = await page.$$eval(selector, (els, names) => els.map((el) => {
    const cs = getComputedStyle(el);
    const heads = el.querySelectorAll("h1, h3");
    const text = heads.length > 1 ? "." + el.classList[0] : (heads[0] ?? el).textContent;
    const label = text.trim().slice(0, 24);
    return `${label.padEnd(24)} ` + names.map((n) => `${n}=${cs.getPropertyValue(n)}`).join(" ");
}), props.split(","));
console.log(rows.join("\n"));
await browser.close();
server.close();
