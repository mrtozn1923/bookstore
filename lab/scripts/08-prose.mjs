// Compares the same CMS body with and without the typography plugin's prose class (08/cms.html).
import { chromium } from "playwright";
import { pathToFileURL } from "node:url";

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });
await page.goto(pathToFileURL("08/cms.html").href);
const style = (sel, prop) => page.$eval(sel, (el, p) => getComputedStyle(el)[p], prop);

for (const id of ["#body-plain", "#body-prose"]) {
    console.log(id);
    console.log(`  article   max-width ${await style(id, "maxWidth")}, font-size ${await style(id, "fontSize")}, line-height ${await style(id, "lineHeight")}`);
    console.log(`  h2        font-size ${await style(`${id} h2`, "fontSize")}, weight ${await style(`${id} h2`, "fontWeight")}, margin-bottom ${await style(`${id} h2`, "marginBottom")}`);
    console.log(`  ul        list-style ${await style(`${id} ul`, "listStyleType")}, padding-inline-start ${await style(`${id} ul`, "paddingInlineStart")}`);
    console.log(`  a         color ${await style(`${id} a`, "color")}, text-decoration ${await style(`${id} a`, "textDecorationLine")}`);
    console.log(`  pre       background ${await style(`${id} pre`, "backgroundColor")}, padding ${await style(`${id} pre`, "padding")}`);
}
console.log(`not-prose button  font-size ${await style("#add-to-cart", "fontSize")}, margin-top ${await style("#add-to-cart", "marginTop")}`);
await page.emulateMedia({ colorScheme: "dark" });
console.log(`dark scheme       prose p color ${await style("#body-prose p", "color")}, plain p color ${await style("#body-plain p", "color")}`);
await browser.close();
