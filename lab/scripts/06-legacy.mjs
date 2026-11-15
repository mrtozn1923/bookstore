// Computed styles of the legacy back-office page; run before and after the v4 upgrade.
import { chromium } from "playwright";
import { pathToFileURL } from "node:url";

const browser = await chromium.launch();
const page = await browser.newPage();
await page.goto(pathToFileURL("06-v3/index.html").href);
const read = (sel, props) => page.$eval(sel, (el, ps) => {
    const s = getComputedStyle(el);
    return ps.map((p) => `${p} ${s[p]}`).join(" | ");
}, props);

console.log("panel  ", await read("#panel", ["borderTopColor", "borderTopLeftRadius", "boxShadow"]));
console.log("search ", await read("#search", ["borderTopColor"]));
console.log("save   ", await read("#save", ["borderTopLeftRadius", "boxShadow", "cursor"]));
await page.focus("#save");
console.log("save:focus", await read("#save", ["boxShadow", "outlineStyle"]));
console.log("badge  ", await read("#badge", ["backgroundColor"]));
console.log("cover  ", await read("#cover", ["flexShrink", "boxShadow"]));
console.log("total  ", await read("#total", ["fontWeight"]));
await browser.close();
