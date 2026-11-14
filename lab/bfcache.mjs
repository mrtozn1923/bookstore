// Back button lab: opens a page, navigates to another page, goes back and reports whether the page
// came from the back/forward cache, how long the way back took and, if not restored, why (CDP).
// Usage: node bfcache.mjs <url> <next-url> [--profile mobile] [--no-store] [--unload] [--cookie]
import { parseArgs } from 'node:util';
import { captureRum, finishVisit, launch, newThrottledPage } from './browser.mjs';

const { values, positionals } = parseArgs({
  allowPositionals: true,
  options: {
    profile: { type: 'string', default: 'mobile' },
    'no-store': { type: 'boolean', default: false },
    unload: { type: 'boolean', default: false },
    cookie: { type: 'boolean', default: false },
  },
});
const [url, nextUrl] = positionals;
const browser = await launch({ full: true });
const { page, cdp } = await newThrottledPage(browser, values.profile);
const readRum = await captureRum(page);
await cdp.send('Page.enable');
const reasons = [];
cdp.on('Page.backForwardCacheNotUsed', (e) => reasons.push(...e.notRestoredExplanations.map((x) => x.reason)));
if (values['no-store']) {
  // As if the server sent the page with Cache-Control: no-store
  await page.route(url, async (route) => {
    const response = await route.fetch();
    await route.fulfill({ response, headers: { ...response.headers(), 'cache-control': 'no-store' } });
  });
}
if (values.unload) {
  await page.addInitScript(() => addEventListener('unload', () => {}));
}
await page.goto(url, { waitUntil: 'networkidle', timeout: 120_000 });
await page.evaluate(() => (window.__marker = 'same page object'));
await page.goto(nextUrl, { waitUntil: 'networkidle', timeout: 120_000 });
// --cookie: the next page changes a cookie (e.g. adds a book to the cart)
if (values.cookie) await page.evaluate(() => (document.cookie = 'cart=9781000000009; path=/'));

const started = Date.now();
await page.goBack({ waitUntil: 'commit' });
await page.locator('app-book-page h1').waitFor();
const ms = Date.now() - started;
const restored = (await page.evaluate(() => window.__marker)) === 'same page object';
// Only the restore is timed here; a full page load is what measure.mjs measures
console.log(`back to ${url.replace(/^https?:\/\/[^/]+/, '')}: ${restored ? `restored from bfcache, book title visible after ${ms} ms` : 'loaded again (a new page load)'}`);
if (reasons.length) console.log(`not restored because (CDP): ${reasons.join(', ')}`);
if (!restored) {
  // The same information for RUM, in Chromium: the navigation entry of the new page load
  const nrr = await page.evaluate(() => performance.getEntriesByType('navigation')[0].notRestoredReasons);
  console.log(`notRestoredReasons: ${JSON.stringify(nrr?.reasons ?? null)}`);
}
await finishVisit(page);
const navTypes = [...new Set((await readRum()).map((r) => `${r.name}:${r.navigationType}`))];
if (restored) console.log(`RUM reports: ${navTypes.join(' ')}`);
await browser.close();
