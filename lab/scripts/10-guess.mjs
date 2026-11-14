// Column count of .book-list around the 48rem (768px) boundary.
import * as sass from "sass";
import { chromium } from "playwright";

const file = process.argv[2] ?? "10/guess.scss";
const { css } = sass.compile(file, { loadPaths: ["10"] });
const browser = await chromium.launch();
for (const width of [767, 768, 769]) {
    const page = await browser.newPage({ viewport: { width, height: 600 } });
    await page.setContent(`<style>body{margin:0}${css}</style><div class="book-list"><p>Dune</p><p>Emma</p><p>Middlemarch</p></div>`);
    const cols = await page.$eval(".book-list", (el) => getComputedStyle(el).gridTemplateColumns.split(" ").length);
    console.log(`${width}px  columns=${cols}`);
    await page.close();
}
await browser.close();
