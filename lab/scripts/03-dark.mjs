// dark: variant with the default strategy (prefers-color-scheme)
import { chromium } from "playwright";
import { pathToFileURL } from "node:url";

const browser = await chromium.launch();
for (const colorScheme of ["light", "dark"]) {
    const page = await browser.newPage({ colorScheme });
    await page.goto(pathToFileURL("03/dark.html").href);
    console.log(colorScheme.padEnd(5), await page.$eval("body", (el) => getComputedStyle(el).backgroundColor));
    await page.close();
}
await browser.close();
