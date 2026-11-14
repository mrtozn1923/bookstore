// Lab run: loads a page N times in fresh contexts with a throttling profile and reads the
// web-vitals values from the app's own RUM beacons (POST /api/rum).
// Usage: node measure.mjs <url> [--runs 5] [--profile mobile] [--type "war"] [--scroll 3] [--until ".row"] [--json]
import { parseArgs } from 'node:util';
import { captureRum, finishVisit, launch, newThrottledPage } from './browser.mjs';

const { values, positionals } = parseArgs({
  allowPositionals: true,
  options: {
    runs: { type: 'string', default: '5' },
    profile: { type: 'string', default: 'mobile' },
    type: { type: 'string' },
    clear: { type: 'boolean', default: false },
    scroll: { type: 'string', default: '0' },
    until: { type: 'string' },
    json: { type: 'boolean', default: false },
  },
});
const url = positionals[0] ?? 'http://127.0.0.1:5300/';

const browser = await launch();
const results = [];
for (let run = 1; run <= Number(values.runs); run++) {
  const { context, page } = await newThrottledPage(browser, values.profile);
  const readRum = await captureRum(page);
  if (values.until) {
    // --until: time (ms since navigation start) when the first element matching the selector appears
    await page.addInitScript((selector) => {
      new MutationObserver((_, observer) => {
        if (document.querySelector(selector)) {
          window.__until = Math.round(performance.now());
          observer.disconnect();
        }
      }).observe(document, { childList: true, subtree: true });
    }, values.until);
  }
  await page.goto(url, { waitUntil: 'networkidle', timeout: 120_000 });
  if (values.type) {
    await page.locator('input[type=search]').pressSequentially(values.type, { delay: 150 });
    // --clear: delete the query again, key by key, like a user starting a new search
    for (let i = 0; values.clear && i < values.type.length; i++) {
      await page.keyboard.press('Backspace');
      await page.waitForTimeout(150);
    }
    await page.waitForTimeout(1000);
  }
  // --scroll: read on, 400 px per wheel step (CLS also counts shifts after the load)
  for (let i = 0; i < Number(values.scroll); i++) {
    await page.mouse.wheel(0, 400);
    await page.waitForTimeout(600);
  }
  await finishVisit(page);
  const entry = Object.fromEntries((await readRum()).map((r) => [r.name, r]));
  if (values.until) entry.UNTIL = { value: await page.evaluate(() => window.__until) };
  results.push(entry);
  await context.close();
}
await browser.close();

if (values.json) {
  console.log(JSON.stringify(results));
  process.exit(0);
}
const median = (xs) => {
  const s = xs.filter((x) => x !== undefined).sort((a, b) => a - b);
  return s.length ? s[Math.floor((s.length - 1) / 2)] : '-';
};
const metrics = ['TTFB', 'FCP', 'LCP', 'CLS', ...(values.type ? ['INP'] : []), ...(values.until ? ['UNTIL'] : [])];
console.log(`${url} profile=${values.profile} runs=${values.runs}`);
console.log('run  ' + metrics.map((m) => m.padStart(7)).join(''));
results.forEach((r, i) =>
  console.log(`${String(i + 1).padEnd(5)}${metrics.map((m) => String(r[m]?.value ?? '-').padStart(7)).join('')}`));
console.log('med  ' + metrics.map((m) => String(median(results.map((r) => r[m]?.value))).padStart(7)).join(''));
const parts = ['ttfb', 'loadDelay', 'loadDuration', 'renderDelay'];
console.log(`LCP subparts (median): ${parts.map((p) => `${p}=${median(results.map((r) => r.LCP?.attribution[p]))}`).join(' ')}`);
console.log(`LCP element: ${results.at(-1).LCP?.attribution.target}`);
if (values.type) {
  const inp = ['inputDelay', 'processing', 'presentation'];
  console.log(`INP subparts (median): ${inp.map((p) => `${p}=${median(results.map((r) => r.INP?.attribution[p]))}`).join(' ')}`);
}
