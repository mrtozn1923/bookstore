// Lists the responses of one page load: encoding, bytes on the wire (Resource Timing transferSize),
// decoded size and Cache-Control. --repeat measures a second visit in the same browser context.
// Usage: node transfer.mjs <url> [--profile mobile] [--repeat]
import { parseArgs } from 'node:util';
import { launch, newThrottledPage } from './browser.mjs';

const { values, positionals } = parseArgs({
  allowPositionals: true,
  options: { profile: { type: 'string', default: 'mobile' }, repeat: { type: 'boolean', default: false } },
});
const browser = await launch();
const { page } = await newThrottledPage(browser, values.profile);
const headers = new Map();
page.on('response', async (res) => headers.set(res.url(), await res.allHeaders()));
const visit = async () => {
  await page.goto(positionals[0], { waitUntil: 'networkidle', timeout: 120_000 });
  return page.evaluate(() =>
    [...performance.getEntriesByType('navigation'), ...performance.getEntriesByType('resource')]
      .filter((e) => e.name.startsWith(location.origin))
      .map((e) => ({ url: e.name, wire: e.transferSize, decoded: e.decodedBodySize })));
};
let rows = await visit();
if (values.repeat) {
  // A new navigation, not a reload: a reload revalidates everything
  await page.goto('about:blank');
  headers.clear();
  rows = await visit();
}
await browser.close();
console.log(`${'response'.padEnd(36)}  enc   wire(kB)  decoded(kB)  cache-control`);
for (const r of rows) {
  const h = headers.get(r.url) ?? {};
  // transferSize 0: served from the HTTP cache without a request
  const wire = r.wire === 0 ? 'cache' : (r.wire / 1000).toFixed(1);
  console.log(`${r.url.replace(/^https?:\/\/[^/]+/, '').slice(0, 36).padEnd(36)}  ${(h['content-encoding'] ?? '-').padEnd(5)} ${wire.padStart(8)}  ${(r.decoded / 1000).toFixed(1).padStart(11)}  ${h['cache-control'] ?? '-'}`);
}
console.log(`bytes on the wire: ${(rows.reduce((a, r) => a + r.wire, 0) / 1000).toFixed(1)} kB in ${rows.filter((r) => r.wire > 0).length} requests`);
