// NOT real users: simulates a small mix of visitors in the lab so the RUM pipeline has data.
// Every session opens one route with a device profile, behaves a little like a user and leaves.
// Usage: node field-sim.mjs [--base http://127.0.0.1:5300]
import { parseArgs } from 'node:util';
import { captureRum, finishVisit, launch, newThrottledPage } from './browser.mjs';

const { values } = parseArgs({ options: { base: { type: 'string', default: 'http://127.0.0.1:5300' } } });
const routes = [
  { path: '/', act: async () => {} },
  { path: '/books', act: async (page) => page.locator('input[type=search]').pressSequentially('war', { delay: 120 }) },
  { path: '/books/9781000000009', act: async () => {} },
];
// Visitor mix: 6 desktop, 6 mid-range phone, 3 low-end phone sessions per route
const mix = [['desktop', 6], ['mobile', 6], ['lowend', 3]];

const browser = await launch();
const samples = [];
for (const route of routes) {
  for (const [profile, count] of mix) {
    for (let i = 0; i < count; i++) {
      const { context, page } = await newThrottledPage(browser, profile);
      const readRum = await captureRum(page);
      await page.goto(values.base + route.path, { waitUntil: 'networkidle', timeout: 120_000 });
      await route.act(page);
      await finishVisit(page);
      samples.push(...(await readRum()));
      await context.close();
      process.stdout.write('.');
    }
  }
}
await browser.close();
console.log(`\n${samples.length} metric reports sent to ${values.base}/api/rum`);

// Why p75 and not the mean: one metric, three summaries per device class
const pct = (sorted, p) => sorted[Math.max(0, Math.ceil(p * sorted.length) - 1)];
for (const device of ['desktop', 'mobile']) {
  const lcp = samples.filter((s) => s.name === 'LCP' && s.route === '/' && s.device === device).map((s) => s.value).sort((a, b) => a - b);
  const mean = Math.round(lcp.reduce((a, b) => a + b, 0) / lcp.length);
  console.log(`LCP on / (${device}, n=${lcp.length}): mean=${mean} p50=${pct(lcp, 0.5)} p75=${pct(lcp, 0.75)} max=${lcp.at(-1)}`);
}
