import { withPage } from './lib/browser.mjs';

// Walks the same checkout task on each version and records what the user would see.
// Usage: node scripts/01-review.mjs [page ...]  (default: before and after)
const customer = { email: 'ada@example.com', fullName: 'Ada Lovelace', address: '12 Moda Street', card: '4111111111111111', expiry: '12/28', cvc: '123' };

async function fill(page, postcode) {
  for (const [name, value] of Object.entries({ ...customer, postcode })) {
    await page.fill(`[name="${name}"]`, value);
  }
}

async function review(page, open, path) {
  await open(path);
  await fill(page, '347');
  await page.click('button[type="submit"]');
  const error = await page.evaluate(() =>
    [...document.querySelectorAll('.message, .field-error')].find((e) => !e.hidden)?.textContent ?? '(none)');
  const emailKept = (await page.inputValue('[name="email"]')) !== '';

  await fill(page, '34710');
  const express = await page.$('input[name="shipping"][value="89.90"]');
  if (express) await express.check(); else await page.selectOption('[name="shipping"]', 'EXP');
  const total = await page.evaluate(() => document.querySelector('#total')?.textContent ?? '(not shown)');

  const before = await page.textContent('button[type="submit"]');
  await page.dblclick('button[type="submit"]');
  await page.waitForTimeout(100);
  const after = await page.textContent('button[type="submit"]');
  await page.waitForTimeout(2000);
  const orders = await page.evaluate(() => window.ordersCreated);
  return { path, error, emailKept, total, feedbackIn100ms: before !== after, orders };
}

await withPage(async ({ page, open }) => {
  const paths = process.argv.slice(2);
  for (const path of paths.length ? paths : ['web/01-checkout/before.html', 'web/01-checkout/after.html']) {
    const r = await review(page, open, path);
    console.log(`${r.path}
  wrong postcode -> message: "${r.error}", email kept: ${r.emailKept}
  total before paying: ${r.total}
  double-click Pay -> feedback within 100 ms: ${r.feedbackIn100ms}, orders created: ${r.orders}`);
  }
}, { width: 440, height: 900 });
