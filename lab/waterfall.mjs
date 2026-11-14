// Prints a text waterfall of one throttled page load from the Resource Timing API:
// start/end of every request (ms since navigation start), render-blocking status, FCP and LCP.
// Usage: node waterfall.mjs <url> [--profile mobile]
import { parseArgs } from 'node:util';
import { launch, newThrottledPage } from './browser.mjs';

const { values, positionals } = parseArgs({
  allowPositionals: true,
  options: { profile: { type: 'string', default: 'mobile' } },
});
const browser = await launch();
const { page } = await newThrottledPage(browser, values.profile);
await page.goto(positionals[0] ?? 'http://127.0.0.1:5300/', { waitUntil: 'networkidle', timeout: 120_000 });
const data = await page.evaluate(async () => {
  // LCP entries are only available through a buffered PerformanceObserver
  const lcp = await new Promise((resolve) =>
    new PerformanceObserver((list) => resolve(list.getEntries().at(-1))).observe({ type: 'largest-contentful-paint', buffered: true }));
  const fcp = performance.getEntriesByName('first-contentful-paint')[0];
  const nav = performance.getEntriesByType('navigation')[0];
  const rows = [nav, ...performance.getEntriesByType('resource')].map((e) => ({
    name: e.name.replace(location.origin, '').replace('http://', '').slice(0, 56),
    start: Math.round(e.startTime),
    end: Math.round(e.responseEnd),
    blocking: e.renderBlockingStatus,
  }));
  return { rows, fcp: Math.round(fcp?.startTime), lcp: Math.round(lcp?.startTime) };
});
await browser.close();

console.log(`${'resource'.padEnd(56)}  start    end  render-blocking`);
for (const r of data.rows) {
  console.log(`${r.name.padEnd(56)} ${String(r.start).padStart(6)} ${String(r.end).padStart(6)}  ${r.blocking}`);
}
console.log(`FCP ${data.fcp} ms, LCP ${data.lcp} ms`);
