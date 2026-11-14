// Can a later, single-class rule override a deeply nested one?
import { chromium } from "playwright";
import * as sass from "sass";

const html = `
<div class="checkout"><section class="order-summary"><ul class="order-summary__items">
    <li><span class="title is-muted">Dune</span><span class="price">₺450.00</span></li>
</ul></section></div>`;
const override = ".is-muted { font-weight: 400; }";

const browser = await chromium.launch();
const page = await browser.newPage();
for (const file of ["02/deep.scss", "02/flat.scss"]) {
    const css = sass.compile(file).css + "\n" + override;
    let markup = html;
    if (file.includes("flat")) {
        markup = html.replace('class="title is-muted"', 'class="order-summary__title is-muted"');
    }
    await page.setContent(`<style>${css}</style>${markup}`);
    const weight = await page.$eval(".is-muted", (el) => getComputedStyle(el).fontWeight);
    console.log(`${file.padEnd(13)} .is-muted → font-weight ${weight}`);
}
await browser.close();
