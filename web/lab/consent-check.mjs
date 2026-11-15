// Three visits: reject, accept, returning visitor with consent.
// Prints whether gtag.js was requested and which gtag calls were queued.
import { chromium } from 'playwright';
const base = process.argv[2];
const browser = await chromium.launch({ args: ['--use-mock-keychain'] });

async function visit(name, setup) {
  const context = await browser.newContext();
  const tagRequests = [];
  await context.route(/googletagmanager\.com|google-analytics\.com/, r => { tagRequests.push(r.request().url()); r.abort(); });
  const page = await context.newPage();
  await setup(page, context);
  const calls = await page.evaluate(() => (window.dataLayer ?? []).map(a => Array.from(a)));
  console.log(`## ${name}: gtag.js requested ${tagRequests.length} time(s)`);
  for (const c of calls) {
    if (c[0] === 'js' || c[0] === 'get') continue;
    console.log('  ' + c.map(x => typeof x === 'object' ? JSON.stringify(x) : x).join(' '));
  }
  await context.close();
}

await visit('reject', async page => {
  await page.goto(base + '/blog/search-console-api');
  await page.click('text=Reject');
  await page.waitForSelector('text=Cookie settings');
});
await visit('accept', async page => {
  await page.goto(base + '/blog/search-console-api');
  await page.click('text=Accept analytics');
  await page.waitForSelector('text=Cookie settings');
});
await visit('returning visitor with consent cookie', async (page, context) => {
  await context.addCookies([{ name: '.AspNet.Consent', value: 'yes', url: base }]);
  await page.goto(base + '/blog/search-console-api');
  await page.waitForSelector('text=Cookie settings');
});
await browser.close();
