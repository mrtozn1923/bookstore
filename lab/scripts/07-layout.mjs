// Measures the catalog grid, book card and cart layout on 07/index.html (Tailwind) and 07/plain.html (plain CSS).
import { chromium } from "playwright";
import { pathToFileURL } from "node:url";

const browser = await chromium.launch();
const boxes = ["#toolbar", "#result-count", "#catalog-grid", "#catalog-grid > li", "#catalog-grid article > div:first-child",
    "#catalog-grid button", "#badge", "#cart-lines", "#summary", "#line-title", "#line-price"];

async function measure(file, width) {
    const page = await browser.newPage({ viewport: { width, height: 900 } });
    await page.goto(pathToFileURL(file).href);
    const data = await page.evaluate((sels) => {
        const cs = (s) => getComputedStyle(document.querySelector(s));
        const rects = sels.flatMap((s) => [...document.querySelectorAll(s)].map((el, i) => {
            const r = el.getBoundingClientRect();
            return [`${s}[${i}]`, [r.x, r.y, r.width, r.height].map(Math.round)];
        }));
        return {
            columns: cs("#catalog-grid").gridTemplateColumns.split(" ").length,
            toolbar: cs("#toolbar").flexDirection,
            count: cs("#result-count").display,
            cart: cs("#cart").display,
            summary: cs("#summary").position,
            rects: Object.fromEntries(rects),
        };
    }, boxes);
    await page.close();
    return data;
}

for (const width of [375, 800, 1280]) {
    const tw = await measure("07/index.html", width);
    const plain = await measure("07/plain.html", width);
    let diff = 0;
    for (const [key, r] of Object.entries(tw.rects)) {
        const p = plain.rects[key];
        diff = Math.max(diff, ...r.map((v, i) => Math.abs(v - p[i])));
    }
    console.log(`${String(width).padStart(4)}px  grid columns ${tw.columns}  toolbar ${tw.toolbar.padEnd(6)}  count ${tw.count.padEnd(5)}  cart ${tw.cart.padEnd(5)}  summary ${tw.summary.padEnd(6)}  boxes ${Object.keys(tw.rects).length}, max diff vs plain CSS ${diff}px`);
}

// pitfalls
const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });
await page.goto(pathToFileURL("07/index.html").href);
for (const id of ["#chips-space", "#chips-gap"]) {
    const r = await page.$eval(id, (box) => {
        const c = box.getBoundingClientRect();
        const items = [...box.children].map((el) => el.getBoundingClientRect());
        const rows = [...new Set(items.map((i) => Math.round(i.top)))];
        const row1 = items.filter((i) => Math.round(i.top) === rows[0]);
        const left = row1[0].left - c.left, right = c.right - row1.at(-1).right;
        const second = items.find((i) => Math.round(i.top) === rows[1]);
        return `rows ${rows.length}, vertical gap ${Math.round(second.top - row1[0].bottom)}px, row 1 space left ${Math.round(left)}px / right ${Math.round(right)}px`;
    });
    console.log(`${id.padEnd(12)} ${r}`);
}
// auto-fill keeps empty tracks; auto-fit collapses them (measured by swapping the value on the grid)
const cardWidth = () => page.$eval("#catalog-grid > li", (el) => Math.round(el.getBoundingClientRect().width));
const fill = await cardWidth();
await page.$eval("#catalog-grid", (el) => { el.style.gridTemplateColumns = "repeat(auto-fit, minmax(12rem, 1fr))"; });
console.log(`4 books at 1280px: auto-fill card ${fill}px, auto-fit card ${await cardWidth()}px`);
await page.$eval("#catalog-grid", (el) => { el.style.gridTemplateColumns = ""; });
// cart columns with and without lg:items-start
const heights = () => page.evaluate(() => ["#cart-lines", "#summary"].map((s) => Math.round(document.querySelector(s).getBoundingClientRect().height)).join("px / ") + "px");
console.log(`cart lines / summary height: with lg:items-start ${await heights()}`);
await page.$eval("#cart", (el) => el.classList.remove("lg:items-start"));
console.log(`cart lines / summary height: without          ${await heights()}`);
await page.$eval("#cart", (el) => el.classList.add("lg:items-start"));
const overflow = async (sel) => page.$eval(sel, (el) => {
    const parent = el.getBoundingClientRect(), price = el.lastElementChild.getBoundingClientRect();
    return `container ${Math.round(parent.width)}px, price right edge ${Math.round(price.right - parent.left)}px`;
});
console.log(`#row-no-min  ${await overflow("#row-no-min")}`);
console.log(`cart line    ${await page.$eval("#line-title", (el) => `title box ${el.clientWidth}px, text ${el.scrollWidth}px`)}`);
const badge = async (sel) => page.$eval(sel, (el) => {
    const b = el.getBoundingClientRect(), p = el.parentElement.getBoundingClientRect();
    return `left ${Math.round(b.left - p.left)}px, right ${Math.round(p.right - b.right)}px`;
});
console.log(`badge ltr    ${await badge("#badge")}`);
console.log(`badge rtl    ${await badge("#badge-rtl")}`);
await browser.close();
