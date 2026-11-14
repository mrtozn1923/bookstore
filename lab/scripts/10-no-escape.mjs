// What the browser keeps from a responsive utility written without the escape.
import * as sass from "sass";
import { chromium } from "playwright";

const { css } = sass.compile("10/no-escape.scss");
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1280, height: 600 } });
await page.setContent(`<style>${css}</style><p class="md:d-block">Dune</p>`);
console.log(`rules kept inside @media: ${await page.evaluate(() => document.styleSheets[0].cssRules[0].cssRules.length)}`);
await browser.close();
