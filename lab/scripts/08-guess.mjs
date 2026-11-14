// Compiles 08/guess.scss and asks Chromium which declarations survive.
import * as sass from "sass";
import { chromium } from "playwright";

const { css } = sass.compile("08/guess.scss");
const browser = await chromium.launch();
const page = await browser.newPage();
await page.setContent(`<style>${css}</style><div class="price-tag">Dune</div>`);
const result = await page.evaluate(() => {
    const rule = document.styleSheets[0].cssRules[0];
    const el = document.querySelector(".price-tag");
    const cs = getComputedStyle(el);
    return {
        kept: rule.style.cssText,
        computed: `line-height=${cs.lineHeight} width=${cs.width}`,
    };
});
console.log(`kept by the browser: ${result.kept}`);
console.log(`computed: ${result.computed}`);
await browser.close();
