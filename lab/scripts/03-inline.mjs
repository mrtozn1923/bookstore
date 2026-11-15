// @theme vs @theme inline when a theme variable points to another variable.
import { chromium } from "playwright";
import { pathToFileURL } from "node:url";

const browser = await chromium.launch();
const page = await browser.newPage();
await page.goto(pathToFileURL("03/inline.html").href);
for (const id of ["plain", "inline"]) {
    console.log(id.padEnd(7), await page.$eval(`#${id}`, (el) => getComputedStyle(el).backgroundColor));
}
await browser.close();
