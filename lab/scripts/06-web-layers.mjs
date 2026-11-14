// Layer order in the built app, and who wins: a layered global rule or an unlayered component rule.
import { createServer } from "node:http";
import { readFile } from "node:fs/promises";
import { extname, join, resolve } from "node:path";
import { chromium } from "playwright";

const root = resolve("../web/dist/bookstore-web/browser");
const types = { ".html": "text/html", ".js": "text/javascript", ".css": "text/css" };
const server = createServer(async (req, res) => {
    const path = req.url === "/" ? "/index.html" : req.url.split("?")[0];
    try {
        res.writeHead(200, { "content-type": types[extname(path)] ?? "application/octet-stream" })
            .end(await readFile(join(root, path)));
    } catch {
        res.writeHead(404).end();
    }
}).listen(0);

const browser = await chromium.launch();
const page = await browser.newPage();
await page.goto(`http://127.0.0.1:${server.address().port}/`);
await page.waitForSelector(".book-card__title");

const order = await page.evaluate(() => [...document.styleSheets]
    .flatMap((sheet) => [...sheet.cssRules])
    .find((rule) => rule instanceof CSSLayerStatementRule).nameList.join(" < "));
console.log(`layer order: ${order}`);

// global rules try to recolor the component's title: (0,3,1) in a layer, then (0,2,1) without one
const attempts = [
    ["layered   (0,3,1)", "@layer utilities { main .book-list .book-card .book-card__title { color: rgb(220, 38, 38); } }"],
    ["unlayered (0,2,1)", "main .book-list .book-card__title { color: rgb(220, 38, 38); }"],
];
for (const [label, css] of attempts) {
    await page.addStyleTag({ content: css });
    const color = await page.$eval(".book-card__title", (el) => getComputedStyle(el).color);
    console.log(`${label} global rule → title color ${color}`);
}

await browser.close();
server.close();
