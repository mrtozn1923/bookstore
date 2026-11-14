// Serves ../web/dist and reads the container-query button and the md:d-block tagline at three widths.
import { createServer } from "node:http";
import { readFile } from "node:fs/promises";
import { extname, join, resolve } from "node:path";
import { chromium } from "playwright";

const root = resolve("../web/dist/bookstore-web/browser");
const types = { ".html": "text/html", ".js": "text/javascript", ".css": "text/css" };
const server = createServer(async (req, res) => {
    const path = req.url === "/" ? "/index.html" : req.url.split("?")[0];
    try {
        res.writeHead(200, { "content-type": types[extname(path)] ?? "application/octet-stream" }).end(await readFile(join(root, path)));
    } catch {
        res.writeHead(404).end();
    }
}).listen(0, "127.0.0.1");

const browser = await chromium.launch();
for (const width of [400, 700, 1280]) {
    const page = await browser.newPage({ viewport: { width, height: 800 } });
    await page.goto(`http://127.0.0.1:${server.address().port}/`);
    await page.waitForSelector(".book-card");
    const card = await page.$eval(".book-card", (el) => el.getBoundingClientRect().width);
    const button = await page.$eval(".book-card__add", (el) => el.getBoundingClientRect().width);
    const tagline = await page.$eval(".text-muted", (el) => getComputedStyle(el).display);
    console.log(`viewport ${String(width).padStart(4)}px  card ${Math.round(card)}px  button ${Math.round(button)}px  tagline ${tagline}`);
    await page.close();
}
await browser.close();
server.close();
