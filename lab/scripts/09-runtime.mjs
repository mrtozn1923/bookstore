// The same hover tone computed at runtime with relative color syntax and color-mix().
import { chromium } from "playwright";

const browser = await chromium.launch();
const page = await browser.newPage();
await page.setContent(`<style>
    :root { --color-brand: #b45309; }
    .relative { color: oklch(from var(--color-brand) calc(l - 0.08) c h); }
    .mix { color: color-mix(in oklch, var(--color-brand), black 15%); }
</style><p class="relative">Dune</p><p class="mix">Emma</p>`);
for (const cls of ["relative", "mix"]) {
    console.log(`${cls.padEnd(9)} ${await page.$eval(`.${cls}`, (el) => getComputedStyle(el).color)}`);
}
await browser.close();
