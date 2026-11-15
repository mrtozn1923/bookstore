// Prints the accessibility tree (Playwright aria snapshot) of one part of a page.
// Usage: node tree.mjs <catalog|book|cart|statement> [css selector] [html]
import { chromium } from 'playwright';
import { open } from './lib.mjs';

const [name = 'catalog', selector = 'body', format] = process.argv.slice(2);
const browser = await chromium.launch();
const page = await (await browser.newContext()).newPage();
await open(page, name);
const target = page.locator(selector).first();
// "html" prints the rendered markup instead (Angular's _ngcontent attributes removed)
console.log(format === 'html'
  ? (await target.evaluate(el => el.outerHTML)).replace(/ _ngcontent-[\w-]+=""/g, '')
  : await target.ariaSnapshot());
await browser.close();
