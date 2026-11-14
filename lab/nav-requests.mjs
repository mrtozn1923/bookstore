// Clicks "View details" on the home page (an Angular router navigation) and logs every request
// after the click with its start and end time (ms after the click) until the book title is visible.
// Usage: node nav-requests.mjs <home-url>
import { launch, newThrottledPage } from './browser.mjs';

const browser = await launch({ full: true });
const { page } = await newThrottledPage(browser, 'mobile');
await page.goto(process.argv[2], { waitUntil: 'networkidle', timeout: 120_000 });
// Without the speculation rules: only the router's own behaviour
await page.evaluate(() => document.querySelectorAll('script[type=speculationrules]').forEach((s) => s.remove()));
await page.waitForTimeout(3000);
let t0 = 0;
page.on('request', (r) => t0 && console.log('req ', Date.now() - t0, r.url().replace(/^https?:\/\/[^/]+/, '')));
page.on('requestfinished', (r) => t0 && console.log('done', Date.now() - t0, r.url().replace(/^https?:\/\/[^/]+/, '')));
t0 = Date.now();
await page.locator('section.hero a').click();
await page.locator('app-book-page h1').waitFor();
console.log('title', Date.now() - t0);
await browser.close();
