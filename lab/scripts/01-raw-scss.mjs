// Feeds the stylesheet to Chromium and WebKit three ways: raw SCSS, raw SCSS without the $brand line, compiled CSS.
import { readFileSync } from "node:fs";
import { chromium, webkit } from "playwright";
import * as sass from "sass";

const source = readFileSync("01/book-card.scss", "utf8");
const compiled = sass.compile("01/book-card.scss").css;
const html = (css) => `<style>${css}</style>
<article class="book-card">
    <h3 class="book-card__title">Dune</h3>
    <p class="price">₺450.00</p>
</article>`;

for (const engine of [chromium, webkit]) {
    const browser = await engine.launch();
    const page = await browser.newPage();
    const withoutVariable = source.replace(/^\$brand.*\n/m, "");
    const cases = [["raw .scss", source], ["raw, no $", withoutVariable], ["compiled", compiled]];
    for (const [label, css] of cases) {
        await page.setContent(html(css));
        const r = await page.evaluate(() => {
            const cs = (sel) => getComputedStyle(document.querySelector(sel));
            return {
                rules: document.styleSheets[0].cssRules.length,
                padding: cs(".book-card").paddingTop,
                title: cs(".book-card__title").color,
                price: cs(".price").fontWeight,
            };
        });
        console.log(`${engine.name().padEnd(8)} ${label.padEnd(9)} rules=${r.rules} padding=${r.padding} title=${r.title} price=${r.price}`);
    }
    await browser.close();
}
