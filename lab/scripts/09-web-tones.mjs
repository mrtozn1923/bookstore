// Serves ../web/dist and reads Bookstore's brand tones in four theme states.
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
for (const scheme of ["light", "dark"]) {
    for (const portal of [false, true]) {
        const page = await browser.newPage({ colorScheme: scheme });
        await page.goto(`http://127.0.0.1:${server.address().port}/`);
        await page.waitForSelector(".book-card");
        if (portal) {
            await page.evaluate(() => document.documentElement.classList.add("institutional"));
        }
        const featured = await page.$eval(".book-card--featured", (el) => getComputedStyle(el).backgroundColor);
        const title = await page.$eval(".book-card__title", (el) => getComputedStyle(el).color);
        await page.hover(".book-card:not(.book-card--featured)");
        const hover = await page.$eval(".book-card:not(.book-card--featured)", (el) => getComputedStyle(el).borderTopColor);
        console.log(`${(scheme + (portal ? " portal" : "")).padEnd(13)} brand=${title.padEnd(18)} hover=${hover.padEnd(18)} subtle=${featured}`);
        await page.close();
    }
}
await browser.close();
server.close();
