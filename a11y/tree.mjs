// Prints the accessibility tree (Playwright aria snapshot) of one part of a page.
// Usage: node tree.mjs <catalog|book|cart> [css selector]
import { chromium } from 'playwright';
import { open } from './lib.mjs';

const [name = 'catalog', selector = 'body'] = process.argv.slice(2);
const browser = await chromium.launch();
const page = await (await browser.newContext()).newPage();
await open(page, name);
console.log(await page.locator(selector).first().ariaSnapshot());
await browser.close();
