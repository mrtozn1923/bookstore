// Records what Bookstore tries to tell a screen reader without moving focus: ariaNotify() calls and text changes
// inside live regions. This is a model of the ARIA rules, not a screen reader.
// Usage: node announce.mjs <book|catalog|busy|checkout|order> [no-arianotify]
import { chromium } from 'playwright';
import { base, focusedAx, open } from './lib.mjs';
import { recorder } from './recorder.mjs';

const [mode = 'book', variant] = process.argv.slice(2);
const browser = await chromium.launch();
const context = await browser.newContext();
// "no-arianotify" removes ariaNotify(), like a browser that does not have it yet
if (variant === 'no-arianotify') {
  await context.addInitScript(() => { delete Document.prototype.ariaNotify; delete Element.prototype.ariaNotify; });
}
await context.addInitScript(recorder);
const page = await context.newPage();

async function flush(label) {
  await page.waitForTimeout(400);
  const entries = await page.evaluate(() => window.__announcements.splice(0));
  console.log(`${label}\n  ${entries.join('\n  ') || '(nothing announced)'}\n  focus: ${await focusedAx(page)}`);
}

if (mode === 'book') {
  await open(page, 'book');
  await flush('page loaded');
  await page.getByRole('button', { name: 'Add to cart' }).focus();
  await page.keyboard.press('Enter');
  await page.getByText('Added.').waitFor();
  await flush('Enter on "Add to cart"');
  await page.keyboard.press('Enter');
  await page.getByTestId('cart-link').filter({ hasText: '(2)' }).waitFor();
  await flush('Enter again');
  if (variant === 'no-arianotify') {
    // The CDK's live element must be hidden on screen; that takes the CDK's a11y CSS
    const box = await page.locator('.cdk-live-announcer-element').boundingBox();
    console.log(`.cdk-live-announcer-element on screen: ${box.width}x${box.height} px`);
  }
}

if (mode === 'catalog') {
  await open(page, 'catalog');
  await flush('page loaded');
  await page.getByRole('radio', { name: 'Science Fiction' }).check();
  await page.getByTestId('book-count').filter({ hasText: '3' }).waitFor();
  await flush('category: Science Fiction');
  await page.getByRole('radio', { name: 'All' }).check();
  const box = page.getByRole('combobox', { name: 'Search books' });
  await box.fill('tolkien');
  // Enter picks the active suggestion; the first one is the typed text itself
  await box.and(page.locator('[aria-activedescendant]')).waitFor();
  await page.keyboard.press('Enter');
  await page.getByTestId('book-count').filter({ hasText: /^1 / }).waitFor();
  await flush('search: tolkien');
  await box.fill('asimov tolkien');
  await box.and(page.locator('[aria-activedescendant]')).waitFor();
  await page.keyboard.press('Enter');
  await page.getByText('No books match').waitFor();
  await flush('search: asimov tolkien');
}

if (mode === 'busy') {
  // A slow network: every book list answer takes one second
  await open(page, 'catalog');
  await page.route('**/api/books?**', async route => { await new Promise(r => setTimeout(r, 1000)); await route.continue(); });
  await page.evaluate(() => window.__announcements.splice(0));
  const cdp = await context.newCDPSession(page);
  // Live region properties of the status node and its container, as Chromium exposes them
  const props = async () => {
    const { nodes } = await cdp.send('Accessibility.getFullAXTree');
    return nodes.filter(n => ['status', 'generic'].includes(n.role?.value) && n.properties?.some(p => ['live', 'busy'].includes(p.name)))
      .map(n => `${n.role.value} "${n.name?.value ?? ''}" ` + n.properties.filter(p => ['live', 'atomic', 'relevant', 'busy'].includes(p.name))
        .map(p => `${p.name}=${p.value.value}`).join(' ')).join('\n  ');
  };
  await page.getByRole('radio', { name: 'Fantasy' }).check();
  await page.getByText('Loading books…').waitFor();
  console.log(`while loading:\n  ${await props()}`);
  await page.getByTestId('book-count').filter({ hasText: 'Fantasy' }).waitFor();
  console.log(`loaded:\n  ${await props()}`);
  await flush('announcements');
}

if (mode === 'checkout') {
  // War and Peace has 3 in stock; ask for 4 so that the API answers 409
  await page.goto(base + '/books/9781400079988');
  await page.getByRole('spinbutton', { name: 'Quantity' }).fill('4');
  await page.getByRole('button', { name: 'Add to cart' }).click();
  await page.getByTestId('cart-link').filter({ hasText: '(4)' }).waitFor();
  await page.goto(base + '/cart');
  await page.locator('table').waitFor();
  await page.evaluate(() => window.__announcements.splice(0));
  await page.getByLabel('E-mail').fill('ada');
  await page.getByLabel('E-mail').press('Enter');
  await flush('submit: name empty, e-mail "ada"');
  await page.getByLabel('Full name').fill('Ada Lovelace');
  await page.getByLabel('E-mail').fill('ada@example.com');
  await page.getByLabel('E-mail').press('Enter');
  await page.waitForResponse(r => r.url().endsWith('/api/orders'));
  await flush('submit: valid form, 4 x War and Peace (3 in stock)');
}

if (mode === 'order') {
  await open(page, 'cart');
  await page.evaluate(() => window.__announcements.splice(0));
  await page.getByLabel('Full name').fill('Ada Lovelace');
  await page.getByLabel('E-mail').fill('ada@example.com');
  await page.getByLabel('E-mail').press('Enter');
  await page.getByTestId('order-confirmation').waitFor();
  await flush('submit: valid order');
}

await browser.close();
