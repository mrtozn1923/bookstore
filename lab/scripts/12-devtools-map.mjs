// Opens the Angular build in Chromium and does what DevTools does: reads the style sheet's
// sourceMappingURL over CDP and maps a rule back to its .scss file and line.
import { createServer } from "node:http";
import { readFile } from "node:fs/promises";
import { extname, join, resolve } from "node:path";
import { chromium } from "playwright";
import { SourceMapConsumer } from "source-map-js";

const root = resolve(process.argv[2] ?? "../web/dist/bookstore-web/browser");
const types = { ".html": "text/html", ".js": "text/javascript", ".css": "text/css", ".map": "application/json" };
const server = createServer(async (req, res) => {
    const path = req.url === "/" ? "/index.html" : req.url.split("?")[0];
    try {
        res.writeHead(200, { "content-type": types[extname(path)] ?? "application/octet-stream" }).end(await readFile(join(root, path)));
    } catch {
        res.writeHead(404).end();
    }
});
await new Promise((done) => server.listen(0, "127.0.0.1", done));
const base = `http://127.0.0.1:${server.address().port}`;

const browser = await chromium.launch();
const page = await browser.newPage();
const cdp = await page.context().newCDPSession(page);
const sheets = [];
cdp.on("CSS.styleSheetAdded", ({ header }) => sheets.push(header));
await cdp.send("DOM.enable");
await cdp.send("CSS.enable");
await page.goto(base);
await page.waitForSelector(".book-card");

const global = sheets.find((h) => h.sourceURL.endsWith(".css"));
console.log(`style sheet: ${global.sourceURL.replace(base, "")}`);
console.log(`sourceMapURL: ${global.sourceMapURL || "(none)"}`);
if (global.sourceMapURL) {
    const css = (await cdp.send("CSS.getStyleSheetText", { styleSheetId: global.styleSheetId })).text;
    const map = new SourceMapConsumer(JSON.parse(await (await fetch(new URL(global.sourceMapURL, global.sourceURL))).text()));
    for (const selector of [".page {", ".book-card", ".md\\:d-block {"]) {
        const index = css.indexOf(selector);
        if (index < 0) continue;
        const before = css.slice(0, index).split("\n");
        const pos = map.originalPositionFor({ line: before.length, column: before.at(-1).length });
        console.log(`${selector.replace(" {", "").padEnd(12)} -> ${pos.source}:${pos.line}`);
    }
}
await browser.close();
server.close();
