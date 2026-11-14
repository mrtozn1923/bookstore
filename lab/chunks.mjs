// Which JavaScript files a visit downloads, and when: page load, browser idle, scroll, click.
// Usage: node chunks.mjs <url> [--profile mobile] [--scroll 2] [--click "text=Export CSV"] [--api]
import { parseArgs } from 'node:util';
import { launch, newThrottledPage } from './browser.mjs';

const { values, positionals } = parseArgs({
  allowPositionals: true,
  options: {
    profile: { type: 'string', default: 'mobile' },
    scroll: { type: 'string', default: '0' },
    click: { type: 'string' },
    api: { type: 'boolean', default: false },
  },
});
const browser = await launch();
const { page } = await newThrottledPage(browser, values.profile);
const t0 = Date.now();
const at = () => String(Date.now() - t0).padStart(6);
page.on('requestfinished', async (req) => {
  // --api: also list the API calls the browser makes (with SSR they should come from the transfer cache)
  if (!req.url().endsWith('.js') && !(values.api && req.url().includes('/api/books'))) return;
  const size = (await req.sizes()).responseBodySize;
  console.log(`${at()} ms  ${req.url().replace(/^https?:\/\/[^/]+/, '').padEnd(42)} ${(size / 1000).toFixed(1).padStart(7)} kB`);
});
await page.goto(positionals[0], { waitUntil: 'networkidle', timeout: 120_000 });
console.log(`${at()} ms  -- load finished, waiting 3 s (browser idle)`);
await page.waitForTimeout(3000);
for (let i = 0; i < Number(values.scroll); i++) {
  console.log(`${at()} ms  -- scroll 400 px`);
  await page.mouse.wheel(0, 400);
  await page.waitForTimeout(1500);
}
if (values.click) {
  console.log(`${at()} ms  -- click ${values.click}`);
  await page.locator(values.click).click();
  await page.waitForTimeout(1500);
}
await browser.close();
