// Checks that the escaped class names match the HTML class attribute.
import * as sass from "sass";
import { chromium } from "playwright";

const { css } = sass.compile("08/escape.scss");
const browser = await chromium.launch();
const page = await browser.newPage();
await page.setContent(`<style>${css}</style><div style="width:600px"><div class="w-1/3">Emma</div><div class="w-2/3">Dune</div></div>`);
for (const cls of ["w-1/3", "w-2/3"]) {
    const width = await page.$eval(`[class="${cls}"]`, (el) => getComputedStyle(el).width);
    console.log(`class="${cls}" width=${width}`);
}
await browser.close();
