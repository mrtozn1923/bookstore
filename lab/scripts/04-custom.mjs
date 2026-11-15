// @custom-variant, compound @variant and a functional @utility in Chromium.
import { chromium } from "playwright";
import { pathToFileURL } from "node:url";

const browser = await chromium.launch();
const page = await browser.newPage();
await page.goto(pathToFileURL("04/custom.html").href);
const style = (sel, prop) => page.$eval(sel, (el, p) => getComputedStyle(el)[p], prop);
console.log(`checkout          bg ${await style("#checkout", "backgroundColor")}`);
await page.focus("#checkout");
await page.keyboard.press("Tab");
await page.keyboard.press("Shift+Tab");
console.log(`checkout (focus)  bg ${await style("#checkout", "backgroundColor")}`);
console.log(`checkout-school   bg ${await style("#checkout-school", "backgroundColor")}, weight ${await style("#checkout-school", "fontWeight")}`);
console.log(`stripes           ${await style("#sold-out", "backgroundImage")}`);
await browser.close();
