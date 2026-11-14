// Speculation Rules lab: hovers over the "View details" link on the home page, prints what Chromium
// prefetched or prerendered (CDP Preload events) and times the click until the book title is visible.
// Usage: node speculation.mjs <url> [--plain] [--no-rules] [--profile mobile]
//   --plain     replace the routerLink with a plain <a href> (a link the Angular router does not handle)
//   --no-rules  remove the page's speculation rules first (baseline)
import { parseArgs } from 'node:util';
import { launch, newThrottledPage } from './browser.mjs';

const { values, positionals } = parseArgs({
  allowPositionals: true,
  options: {
    plain: { type: 'boolean', default: false },
    'no-rules': { type: 'boolean', default: false },
    profile: { type: 'string', default: 'mobile' },
  },
});
const browser = await launch({ full: true });
const { page, cdp } = await newThrottledPage(browser, values.profile);
await cdp.send('Preload.enable');
const events = [];
cdp.on('Preload.prerenderStatusUpdated', (e) => events.push(`prerender ${e.status}${e.prerenderStatus ? ` (${e.prerenderStatus})` : ''}`));
cdp.on('Preload.prefetchStatusUpdated', (e) => events.push(`prefetch ${e.status}${e.prefetchStatus ? ` (${e.prefetchStatus})` : ''}`));

await page.goto(positionals[0], { waitUntil: 'networkidle', timeout: 120_000 });
if (values['no-rules']) {
  await page.evaluate(() => document.querySelectorAll('script[type=speculationrules]').forEach((s) => s.remove()));
}
await page.evaluate(() => (window.__marker = 'home page document'));
const link = page.locator('section.hero a');
if (values.plain) {
  // cloneNode drops Angular's click listener: the browser handles the click as a document navigation
  await link.evaluate((a) => a.replaceWith(a.cloneNode(true)));
}
await link.hover();
await page.waitForTimeout(3000);
const started = Date.now();
await link.click();
await page.locator('app-book-page h1').waitFor();
const ms = Date.now() - started;
const nav = await page.evaluate(() => {
  const n = performance.getEntriesByType('navigation')[0];
  return { same: window.__marker === 'home page document', type: n.deliveryType || '(network)', ttfb: Math.round(n.responseStart) };
});
await browser.close();
console.log(`speculation: ${events.length ? [...new Set(events)].join(', ') : 'none'}`);
console.log(nav.same
  ? `click -> book title after ${ms} ms, same document (Angular router navigation)`
  : `click -> book title after ${ms} ms, new document: delivery ${nav.type}, TTFB ${nav.ttfb} ms`);
