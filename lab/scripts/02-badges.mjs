// Reads the computed colors of the category and stock badges on Bookstore.Web's catalog page.
import { chromium } from "playwright";

const url = process.argv[2] ?? "http://127.0.0.1:52861/";
const browser = await chromium.launch();
const page = await browser.newPage();
await page.goto(url);
const rows = await page.$$eval("article", (cards) => cards.map((card) => {
    const read = (sel) => {
        const el = card.querySelector(sel);
        if (!el) return "-";
        const s = getComputedStyle(el);
        return `bg ${s.backgroundColor}, text ${s.color}`;
    };
    return `${card.querySelector("h3").textContent.padEnd(10)} category: ${read(".category-badge")} | stock: ${read(".stock-badge")}`;
}));
rows.forEach((r) => console.log(r));
await browser.close();
