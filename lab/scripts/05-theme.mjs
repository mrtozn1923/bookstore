// Reads the same three colors under each OS preference and after each click on the theme button.
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

async function read(page, label) {
    const r = await page.evaluate(() => {
        const cs = (sel) => getComputedStyle(document.querySelector(sel));
        return {
            html: document.documentElement.className || "-",
            body: cs("body").backgroundColor,
            title: cs(".book-card__title").color,
            button: cs(".book-card__add").backgroundColor,
        };
    });
    console.log(`${label.padEnd(22)} class=${r.html.padEnd(5)} body=${r.body.padEnd(18)} title=${r.title.padEnd(18)} button=${r.button}`);
}

const browser = await chromium.launch();
for (const scheme of ["light", "dark"]) {
    const page = await browser.newPage({ colorScheme: scheme });
    await page.goto(`http://127.0.0.1:${server.address().port}/`);
    await page.waitForSelector(".book-card__add");
    await read(page, `OS ${scheme}`);
    for (const next of ["light", "dark", "system"]) {
        await page.getByRole("button", { name: /Theme:/ }).click();
        await page.getByRole("button", { name: `Theme: ${next}` }).waitFor();
        await page.evaluate(() => new Promise(requestAnimationFrame));
        await read(page, `OS ${scheme}, theme ${next}`);
    }
    await page.close();
}
await browser.close();
server.close();
