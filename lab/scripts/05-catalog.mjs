// bookstore-web catalog: stock badges, the Angular Aria format filter and component CSS.
import { chromium } from "playwright";

const url = process.argv[2] ?? "http://127.0.0.1:52862/";
const browser = await chromium.launch();
const page = await browser.newPage();
await page.goto(url);
await page.waitForSelector("app-book-card");
const settle = () => page.waitForTimeout(150);

for (const card of await page.$$("app-book-card")) {
    console.log(await card.evaluate((el) => {
        const badge = el.querySelector(".stock-badge");
        const bg = getComputedStyle(badge).backgroundColor;
        const opacity = getComputedStyle(el.querySelector("article")).opacity;
        return `${el.querySelector("h3").textContent.padEnd(11)} ${badge.textContent.trim().padEnd(12)} bg ${bg}  card opacity ${opacity}`;
    }));
}

async function options(label) {
    await settle();
    const state = await page.$$eval("[ngOption]", (els) => els.map((e) =>
        `${e.textContent.trim()}[selected=${e.getAttribute("aria-selected")} active=${e.dataset.active} bg ${getComputedStyle(e).backgroundColor}]`).join(" "));
    console.log(`${label.padEnd(16)} ${state} -> ${await page.$eval("main p.my-4", (e) => e.textContent)}`);
}

await options("load");
await page.locator("[ngOption]", { hasText: "Paperback" }).click();
await options("click Paperback");
await page.keyboard.press("ArrowRight");
await page.keyboard.press("Space");
await options("ArrowRight Space");
console.log("focused:", await page.evaluate(() => {
    const el = document.activeElement;
    const s = getComputedStyle(el);
    return `${el.textContent.trim()}, outline ${s.outlineStyle} ${s.outlineWidth}`;
}));
console.log(".price:", await page.$eval(".price", (e) => {
    const s = getComputedStyle(e);
    return `${s.fontSize} ${s.fontWeight} ${s.color}`;
}));
await browser.close();
