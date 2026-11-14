// The same two rules with and without cascade layers.
import { chromium } from "playwright";
import * as sass from "sass";

const html = `<div class="datepicker"><table><tr>
    <td class="calendar-day calendar-day--selected is-selected">14</td>
</tr></table></div>`;

const browser = await chromium.launch();
const page = await browser.newPage();
for (const file of ["06/main-unlayered.scss", "06/main-layered.scss"]) {
    await page.setContent(`<style>${sass.compile(file).css}</style>${html}`);
    const bg = await page.$eval("td", (el) => getComputedStyle(el).backgroundColor);
    console.log(`${file.padEnd(22)} selected day background=${bg}`);
}
await browser.close();
