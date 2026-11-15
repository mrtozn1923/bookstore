// An old unlayered rule against a Tailwind utility: which color wins?
import { chromium } from "playwright";
import { pathToFileURL } from "node:url";

const browser = await chromium.launch();
const page = await browser.newPage();
await page.goto(pathToFileURL("01/unlayered.html").href);
console.log("h3.text-slate-900 color:", await page.$eval("#title", (el) => getComputedStyle(el).color));
await browser.close();
