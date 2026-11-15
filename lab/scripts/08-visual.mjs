// Measures the visual utilities on 08/index.html: hover shadow and scale, grayscale, text-balance, borders, rings, outlines, scroll snap.
import { chromium } from "playwright";
import { pathToFileURL } from "node:url";

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1280, height: 1000 } });
await page.goto(pathToFileURL("08/index.html").href);
const style = (sel, prop, pseudo = null) => page.$eval(sel, (el, [p, ps]) => getComputedStyle(el, ps)[p], [prop, pseudo]);
const cover = "#card-dune .aspect-2\\/3";
// box-shadow lists every layer (inset, ring, shadow); drop the empty ones
const layers = (v) => v.split(/,\s(?![^()]*\))/).filter((l) => l !== "rgba(0, 0, 0, 0) 0px 0px 0px 0px").join(", ");

// 1) hover: card shadow and cover scale
console.log(`dune before hover  shadow ${layers(await style("#card-dune", "boxShadow"))}  cover scale ${await style(cover, "scale")}`);
await page.hover("#card-dune");
await page.waitForTimeout(400);
console.log(`dune after hover   shadow ${layers(await style("#card-dune", "boxShadow"))}  cover scale ${await style(cover, "scale")}`);
await page.mouse.move(0, 0);

// 2) out of stock
console.log(`emma cover         filter ${await style("#card-emma .aspect-2\\/3", "filter")}  opacity ${await style("#card-emma .aspect-2\\/3", "opacity")}`);
console.log(`emma overlay       background ${await style("#card-emma span", "backgroundColor")}`);
console.log(`emma blurb         mask-image ${(await style("#blurb", "maskImage")).slice(0, 60)}...`);

// 3) promo: gradient, text-balance, text-shadow
console.log(`promo background   ${(await style("#promo", "backgroundImage")).slice(0, 72)}...`);
for (const id of ["#promo-plain", "#promo-balance"]) {
    const lines = await page.$eval(id, (el) => {
        const range = document.createRange();
        range.selectNodeContents(el);
        const rows = new Map();
        for (const r of range.getClientRects()) rows.set(Math.round(r.top), Math.max(rows.get(Math.round(r.top)) ?? 0, r.right - el.getBoundingClientRect().left));
        return [...rows.values()].map((w) => `${Math.round(w)}px`).join(" / ");
    });
    console.log(`${id.padEnd(18)} text-wrap ${(await style(id, "textWrap")).padEnd(7)} line widths ${lines}`);
}
console.log(`#promo-balance     text-shadow ${(await style("#promo-balance", "textShadow")).slice(0, 48)}...`);

// 4) borders, rings, outlines
console.log(`border             color ${await style("#border-default", "color")}  border-color ${await style("#border-default", "borderTopColor")}  width ${await style("#border-default", "borderTopWidth")}`);
console.log(`ring               box-shadow ${layers(await style("#ring-default", "boxShadow"))}`);
for (const forced of ["none", "active"]) {
    await page.emulateMedia({ forcedColors: forced });
    const o = async (sel) => (await style(sel, "outlineStyle")) === "none" ? "none" : `${await style(sel, "outlineStyle")} ${await style(sel, "outlineWidth")}`;
    console.log(`forced-colors ${forced.padEnd(6)} outline-none: ${await o("#btn-none")}  outline-hidden: ${await o("#btn-hidden")}`);
}
await page.emulateMedia({ forcedColors: "none" });

// 5) reduced motion
await page.emulateMedia({ reducedMotion: "reduce" });
console.log(`reduced motion     cover transition-property ${await style(cover, "transitionProperty")}`);
await page.emulateMedia({ reducedMotion: "no-preference" });

// 6) scroll snap
await page.$eval("#carousel", (el) => el.scrollBy({ left: 70 }));
await page.waitForTimeout(600);
console.log(`carousel           scrolled 70px, scrollLeft after snap ${await page.$eval("#carousel", (el) => el.scrollLeft)}px, scrollbar-width ${await style("#carousel", "scrollbarWidth")}`);
await browser.close();
