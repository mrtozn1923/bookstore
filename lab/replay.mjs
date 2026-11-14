// Event replay: clicks "Show more" in the server-rendered HTML before the app has hydrated and
// checks whether Angular replays the click once the block is hydrated.
// Usage: node replay.mjs <url> [--profile lowend]
import { parseArgs } from 'node:util';
import { launch, newThrottledPage } from './browser.mjs';

const { values, positionals } = parseArgs({
  allowPositionals: true,
  options: { profile: { type: 'string', default: 'lowend' } },
});
const browser = await launch();
const { page } = await newThrottledPage(browser, values.profile);
await page.goto(positionals[0], { waitUntil: 'commit' });
await page.locator('button.more').waitFor();
const clickedAt = await page.evaluate(() => performance.now());
await page.locator('button.more').click();
const mainDone = await page.evaluate(() =>
  performance.getEntriesByType('resource').find((e) => /\/main-.*\.js$/.test(e.name))?.responseEnd ?? null);
const replayed = await page
  .waitForFunction(() => document.querySelectorAll('app-similar-books li').length === 8, null, { timeout: 15_000 })
  .then(() => true, () => false);
const doneAt = await page.evaluate(() => performance.now());
const cards = await page.evaluate(() => document.querySelectorAll('app-similar-books li').length);
const chunk = await page.evaluate(() =>
  performance.getEntriesByType('resource').filter((e) => e.name.endsWith('.js')).map((e) => Math.round(e.responseEnd)));
await browser.close();
console.log(`click on "Show more" at ${Math.round(clickedAt)} ms (main.js downloaded: ${mainDone === null ? 'no' : Math.round(mainDone) + ' ms'})`);
console.log(replayed
  ? `8 cards visible at ${Math.round(doneAt)} ms; JavaScript files finished at ${chunk.join(', ')} ms`
  : `after 15 s still ${cards} cards: the click was not replayed; JavaScript files finished at ${chunk.join(', ')} ms`);
