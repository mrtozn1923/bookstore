// How Chromium renders an out-of-gamut hsl() from Sass versus the gamut-mapped color.
import * as sass from "sass";
import { chromium } from "playwright";

const { css } = sass.compile("09/gamut.scss");
const values = Object.fromEntries([...css.matchAll(/^\s+(\w[\w-]*): (.+);$/gm)].map((m) => [m[1], m[2]]));
const browser = await chromium.launch();
const page = await browser.newPage();
for (const name of ["raw", "mapped", "clipped"]) {
    await page.setContent(`<div style="color: ${values[name]}">Dune</div>`);
    const color = await page.$eval("div", (el) => getComputedStyle(el).color);
    console.log(`${name.padEnd(8)} ${color}`);
}
await browser.close();
