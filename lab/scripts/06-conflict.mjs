// Two conflicting utilities on one element: which one wins?
import { chromium } from "playwright";
import { pathToFileURL } from "node:url";

const browser = await chromium.launch();
const page = await browser.newPage();
await page.goto(pathToFileURL("06/conflict.html").href);
for (const id of ["a", "b", "c"]) {
    const r = await page.$eval(`#${id}`, (el) => {
        const s = getComputedStyle(el);
        return `class="${el.className}" → bg ${s.backgroundColor}, padding-inline ${s.paddingLeft}, display ${s.display}`;
    });
    console.log(r);
}
await browser.close();
