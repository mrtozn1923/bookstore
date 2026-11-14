// The same nested file, once as native CSS nesting and once compiled by Sass.
import { readFileSync } from "node:fs";
import { chromium, webkit } from "playwright";
import * as sass from "sass";

const file = "02/list-parent.scss";
const cases = [
    ["native nesting", readFileSync(file, "utf8")],
    ["compiled by Sass", sass.compile(file).css],
];
const body = `<article class="book-card"><p class="price">₺450.00</p></article>`;

for (const engine of [chromium, webkit]) {
    const browser = await engine.launch();
    const page = await browser.newPage();
    for (const [label, css] of cases) {
        await page.setContent(`<style>${css}</style>${body}`);
        const weight = await page.$eval(".price", (el) => getComputedStyle(el).fontWeight);
        console.log(`${engine.name().padEnd(8)} ${label.padEnd(17)} .book-card .price → font-weight ${weight}`);
    }
    await browser.close();
}
