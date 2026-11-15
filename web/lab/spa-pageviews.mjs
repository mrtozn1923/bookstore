// Drives the built Angular app and prints the gtag calls queued in dataLayer.
// Requests to Google are blocked: nothing leaves the machine.
import { chromium } from 'playwright';
const base = process.argv[2];
const browser = await chromium.launch({ args: ['--use-mock-keychain'] });
const page = await browser.newPage();
await page.route(/googletagmanager\.com|google-analytics\.com/, r => r.abort());
await page.goto(base + '/');
await page.click('text=Search Console API in C#');
await page.waitForURL('**/blog/search-console-api');
await page.click('text=Copy');
await page.click('text=Next post');
await page.waitForURL('**/blog/weekly-report');
const calls = await page.evaluate(() => window.dataLayer.map(a => Array.from(a)));
for (const c of calls) {
  if (c[0] === 'js') { console.log('js', '<Date>'); continue; }
  if (c[0] === 'get') { console.log('get', c[1], c[2], '<callback>'); continue; }
  console.log(c.map(x => typeof x === 'object' ? JSON.stringify(x) : x).join(' '));
}
await browser.close();
