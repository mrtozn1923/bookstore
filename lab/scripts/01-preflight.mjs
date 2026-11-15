// What does Preflight do to HTML that comes from the CMS without classes?
import { chromium } from "playwright";
import { pathToFileURL } from "node:url";

const browser = await chromium.launch();
const page = await browser.newPage();
await page.goto(pathToFileURL("01/cms.html").href);
const read = (sel) => page.$eval(sel, (el) => {
    const s = getComputedStyle(el);
    return `font-size ${s.fontSize}, weight ${s.fontWeight}, margin-bottom ${s.marginBottom}, list-style ${s.listStyleType}`;
});
console.log("h1 (class)   :", await read("h1"));
console.log("cms h2       :", await read("#cms-body h2"));
console.log("cms p        :", await read("#cms-body p"));
console.log("cms ul       :", await read("#cms-body ul"));
await browser.close();
