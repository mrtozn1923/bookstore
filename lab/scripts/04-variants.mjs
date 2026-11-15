// Measures the variants on 04/index.html in Chromium.
import { chromium } from "playwright";
import { pathToFileURL } from "node:url";

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
await page.goto(pathToFileURL("04/index.html").href);
const style = (sel, prop) => page.$eval(sel, (el, p) => getComputedStyle(el)[p], prop);

// 1) hover the section's padding, not the card
for (const [section, title] of [["#featured", "#title-plain"], ["#featured-named", "#title-named"]]) {
    const box = await page.locator(section).boundingBox();
    await page.mouse.move(box.x + 8, box.y + 8);
    console.log(`hover ${section.padEnd(16)} ${title} color ${await style(title, "color")}`);
}
await page.mouse.move(0, 0);

// 2) filters
await page.click("label[for=in-stock]");
await page.click("#hardcover input");
console.log(`peer-checked label bg  ${await style("label[for=in-stock]", "backgroundColor")}`);
console.log(`has-checked label bg   ${await style("#hardcover", "backgroundColor")}`);

// 3) disclosure
for (let i = 0; i < 2; i++) {
    console.log(`aria-expanded=${await page.getAttribute("#format-button", "aria-expanded")}  chevron rotate ${await style("#chevron", "rotate")}  panel display ${await style("#format-panel", "display")}`);
    await page.click("#format-button");
    await page.waitForTimeout(200);
}

// 4) container queries
for (const id of ["narrow", "wide"]) {
    console.log(`@container #${id.padEnd(6)} width ${await style(`#${id}`, "width")}  article flex-direction ${await style(`#${id} article`, "flexDirection")}`);
}
console.log(`h-[50cqb] in @container-size: ${await style("#half", "height")}, in @container: ${await style("#half-inline", "height")}`);

// 5) popover enter transition
await page.click("button[popovertarget=cart-preview]");
const anims = await page.$eval("#cart-preview", (el) => el.getAnimations().map((a) => `${a.transitionProperty} ${a.effect.getKeyframes().map((k) => k[a.transitionProperty]).join(" -> ")}`));
console.log(`popover open, running transitions: ${anims.join(", ")}`);

// 6) logical properties and scrollbar
console.log(`rtl ps-4: padding-left ${await style("#rtl-note", "paddingLeft")}, padding-right ${await style("#rtl-note", "paddingRight")}, border-right-width ${await style("#rtl-note", "borderRightWidth")}`);
console.log(`scrollbar: width ${await style("#order-list", "scrollbarWidth")}, color ${await style("#order-list", "scrollbarColor")}`);
await browser.close();
