// Dark mode in Bookstore.Web: OS preference, stored choice and the toggle button.
import { chromium } from "playwright";

const url = process.argv[2] ?? "http://127.0.0.1:52861/";
const browser = await chromium.launch();

async function check(label, colorScheme, stored, click = false) {
    const context = await browser.newContext({ colorScheme });
    if (stored) await context.addInitScript((v) => localStorage.setItem("theme", v), stored);
    const page = await context.newPage();
    await page.goto(url);
    if (click) await page.click("[data-theme-toggle]");
    const r = await page.evaluate(() => ({
        dark: document.documentElement.classList.contains("dark"),
        body: getComputedStyle(document.body).backgroundColor,
        card: getComputedStyle(document.querySelector("article")).backgroundColor,
        stored: localStorage.getItem("theme"),
    }));
    console.log(`${label.padEnd(26)} dark=${String(r.dark).padEnd(5)} body ${r.body}  card ${r.card}  stored=${r.stored}`);
    await context.close();
}

await check("OS light, nothing stored", "light", null);
await check("OS dark, nothing stored", "dark", null);
await check("OS dark, stored light", "dark", "light");
await check("OS light, toggle clicked", "light", null, true);
await browser.close();
