// Opens a post, ends the visit like a user switching tabs and prints the beacon the page sent.
// Values come from one headless Chromium run on this machine: lab data, not field data.
import { chromium } from 'playwright';
const base = process.argv[2];
const browser = await chromium.launch({ args: ['--use-mock-keychain'] });
const page = await browser.newPage();
const beacon = page.waitForRequest(r => r.url().endsWith('/rum') && r.method() === 'POST');
await page.goto(base + '/blog/search-console-api');
await page.waitForTimeout(500);
await page.keyboard.press('Shift'); // an input ends LCP observation
await page.evaluate(() => {
  Object.defineProperty(document, 'visibilityState', { value: 'hidden', configurable: true });
  document.dispatchEvent(new Event('visibilitychange'));
});
const request = await beacon;
// the body is printed by serve.mjs
console.log('beacon sent:', request.method(), new URL(request.url()).pathname, request.headers()['content-type']);
await browser.close();
