// Measures the checkout form on 08/checkout.html without and with @tailwindcss/forms.
import { chromium } from "playwright";
import { pathToFileURL } from "node:url";

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });
const style = (sel, prop, pseudo = null) => page.$eval(sel, (el, [p, ps]) => getComputedStyle(el, ps)[p], [prop, pseudo]);

for (const css of ["checkout.out.css", "checkout-forms.out.css"]) {
    await page.goto(pathToFileURL("08/checkout.html").href);
    await page.$eval("#css", (el, href) => { el.href = href; }, css);
    await page.waitForLoadState("load");
    await page.waitForTimeout(200);
    console.log(css);
    console.log(`  email     appearance ${await style("#email", "appearance")}, font-size ${await style("#email", "fontSize")}, placeholder ${await style("#email", "color", "::placeholder")}`);
    console.log(`  select    appearance ${await style("#shipping", "appearance")}, padding-right ${await style("#shipping", "paddingRight")}, background-image ${(await style("#shipping", "backgroundImage")).slice(0, 30)}`);
    const gift = async () => `appearance ${await style("#gift", "appearance")}, ${await style("#gift", "width")}, background ${await style("#gift", "backgroundColor")}`;
    console.log(`  checkbox  ${await gift()}`);
    await page.check("#gift");
    console.log(`  checked   ${await gift()}, accent-color ${await style("#gift", "accentColor")}`);
    await page.check("#invoice");
    console.log(`  invoice   checked, background ${await style("#invoice", "backgroundColor")}, accent-color ${await style("#invoice", "accentColor")}`);
    const before = await style("#note", "height");
    await page.fill("#note", "Leave at the door.\nRing twice.\nThanks, Ada");
    console.log(`  textarea  field-sizing ${await style("#note", "fieldSizing")}, height ${before} -> ${await style("#note", "height")}`);
    console.log(`  file      button background ${await style("#po", "backgroundColor", "::file-selector-button")}, margin-inline-end ${await style("#po", "marginInlineEnd", "::file-selector-button")}`);
    console.log(`  submit    disabled, cursor ${await style("#place-order", "cursor")}, opacity ${await style("#place-order", "opacity")}`);
}
await browser.close();
