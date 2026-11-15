// bookstore-web: the appButton variants and a caller's class override.
import { chromium } from "playwright";

const url = process.argv[2] ?? "http://127.0.0.1:52862/";
const browser = await chromium.launch();
const page = await browser.newPage();
await page.goto(url);
await page.waitForSelector("app-book-card");
await page.waitForTimeout(150);

const clear = await page.$eval("#clear-filters", (el) => {
    const s = getComputedStyle(el);
    return `has px-3: ${el.classList.contains("px-3")}, has px-2: ${el.classList.contains("px-2")}, padding-left ${s.paddingLeft}, margin-left ${s.marginLeft === "0px" ? "0px" : "auto"}`;
});
console.log(`Clear (intent=secondary size=sm class="ml-auto px-2") → ${clear}`);
const cards = await page.$$eval("app-book-card button", (els) => els.map((el) => {
    const s = getComputedStyle(el);
    return `${el.textContent.trim().padEnd(11)} disabled=${String(el.disabled).padEnd(5)} bg ${s.backgroundColor}  opacity ${s.opacity}  cursor ${s.cursor}`;
}));
cards.forEach((c) => console.log(c));
await browser.close();
