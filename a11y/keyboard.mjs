// Uses Bookstore with the keyboard only and prints where focus goes.
// Usage: node keyboard.mjs <tabs|route|skip|sticky|radios|dialog|filters>
import { chromium } from 'playwright';
import { base, open } from './lib.mjs';

const browser = await chromium.launch();
const page = await (await browser.newContext({ viewport: { width: 1280, height: 600 } })).newPage();

// Role and name of the focused element, as a screen reader would get them
async function focused() {
  const el = page.locator(':focus');
  if (await el.count() === 0) return '(nothing: document.body)';
  return (await el.ariaSnapshot()).split('\n')[0].replace(/^- /, '').replace(/:$/, '');
}

async function tabUntil(text, max = 60) {
  for (let i = 1; i <= max; i++) {
    await page.keyboard.press('Tab');
    if ((await focused()).includes(text)) return i;
  }
  return -1;
}

const mode = process.argv[2] ?? 'tabs';

if (mode === 'tabs') {
  // How many Tab presses from page load to "Add to cart" for Dune?
  await open(page, 'catalog');
  const stops = [];
  for (let i = 0; i < 9; i++) {
    await page.keyboard.press('Tab');
    stops.push(await focused());
  }
  console.log(stops.map((s, i) => `${String(i + 1).padStart(2)} ${s}`).join('\n'));
  await open(page, 'catalog');
  const dune = page.locator('.book-card', { hasText: 'Dune' }).getByRole('button');
  for (let i = 1; i <= 60; i++) {
    await page.keyboard.press('Tab');
    if (await dune.evaluate(el => el === document.activeElement)) {
      console.log(`Tab presses to "Add to cart" for Dune: ${i}`);
      break;
    }
  }
  // Same task, but start with the skip link: Tab, Enter, then Tab from the top of main
  await open(page, 'catalog');
  await page.keyboard.press('Tab');
  if ((await focused()).includes('Skip to main content')) {
    await page.keyboard.press('Enter');
    for (let i = 1; i <= 60; i++) {
      await page.keyboard.press('Tab');
      if (await dune.evaluate(el => el === document.activeElement)) {
        console.log(`with the skip link: 1 Tab + Enter + ${i} Tab`);
        break;
      }
    }
  }
}

if (mode === 'route') {
  // Open a book with the keyboard: where is focus after the route changes?
  await open(page, 'catalog');
  await tabUntil('link "Dune"');
  await page.keyboard.press('Enter');
  await page.getByRole('heading', { level: 1, name: 'Dune' }).waitFor();
  console.log(`url=${new URL(page.url()).pathname} title="${await page.title()}"`);
  console.log(`focus after Enter: ${await focused()}`);
  await page.keyboard.press('Tab');
  console.log(`next Tab:         ${await focused()}`);
}

if (mode === 'skip') {
  // Activate the skip link on the cart page: does focus land in main, and do we stay on /cart?
  await open(page, 'cart');
  await page.keyboard.press('Tab');
  console.log(`first Tab: ${await focused()}`);
  await page.keyboard.press('Enter');
  await page.waitForTimeout(500);
  console.log(`after Enter: url=${new URL(page.url()).pathname}${new URL(page.url()).hash} focus=${await focused()}`);
  await page.keyboard.press('Tab');
  console.log(`next Tab: ${await focused()}`);
}

if (mode === 'sticky') {
  // Shift+Tab back up the catalog: is the focused element hidden under the sticky header?
  await open(page, 'catalog');
  await page.locator('.book-card').last().getByRole('button').focus();
  const header = await page.locator('.site-header').boundingBox();
  let hidden = 0, partly = 0, checked = 0;
  for (let i = 0; i < 60; i++) {
    await page.keyboard.press('Shift+Tab');
    const box = await page.locator(':focus').boundingBox();
    if (!box) break;
    const isHeader = await page.locator(':focus').evaluate(el => !!el.closest('.site-header'));
    if (isHeader) break;
    checked++;
    const covered = Math.min(box.y + box.height, header.y + header.height) - Math.max(box.y, header.y);
    if (covered >= box.height) hidden++;
    else if (covered > 0) partly++;
  }
  console.log(`header height=${header.height}px, focused elements checked=${checked}, fully hidden=${hidden}, partly hidden=${partly}`);
}

if (mode === 'radios') {
  // The category chips are native radio buttons: one Tab stop, arrow keys move inside the group
  await open(page, 'catalog');
  await tabUntil('radio "All"');
  console.log(`Tab reaches: ${await focused()}`);
  for (let i = 0; i < 2; i++) {
    const response = page.waitForResponse(r => r.url().includes('/api/books?'));
    await page.keyboard.press('ArrowRight');
    await response;
    await page.getByTestId('book-count').waitFor();
    console.log(`ArrowRight: ${await focused()}  ${await page.getByTestId('book-count').textContent()}`);
  }
  await page.keyboard.press('Tab');
  console.log(`Tab leaves the group: ${await focused()}`);
  await page.keyboard.press('Shift+Tab');
  console.log(`Shift+Tab comes back to: ${await focused()}`);
}

if (mode === 'dialog') {
  // Two books in the cart, then remove the first one through the confirmation dialog, keyboard only
  await open(page, 'cart');
  await page.goto(base + '/');
  await page.locator('.book-card', { hasText: 'Dune' }).getByRole('button').click();
  await page.getByTestId('cart-link').filter({ hasText: '(2)' }).waitFor();
  await page.goto(base + '/cart');
  await page.locator('tbody tr').nth(1).waitFor();
  await tabUntil('button "Remove');
  console.log(`trigger: ${await focused()} (row 1 of 2)`);
  await page.keyboard.press('Enter');
  console.log(`dialog open, focus: ${await focused()}`);
  const inDialog = [];
  for (let i = 0; i < 3; i++) {
    await page.keyboard.press('Tab');
    inDialog.push(await focused());
  }
  console.log(`Tab x3: ${inDialog.join(' -> ')}`);
  await page.locator('.brand').evaluate(el => el.focus());
  console.log(`page link .focus() while open: ${await focused()}`);
  await page.keyboard.press('Escape');
  console.log(`Escape: focus=${await focused()}`);
  await page.keyboard.press('Enter');
  await page.keyboard.press('Tab');
  console.log(`reopen + Tab: ${await focused()}`);
  await page.keyboard.press('Enter');
  await page.locator('tbody tr').nth(1).waitFor({ state: 'detached' });
  await page.waitForTimeout(200);
  console.log(`confirmed, rows left=${await page.locator('tbody tr').count()}, focus=${await focused()}`);
}

if (mode === 'filters') {
  // The Filters button is a disclosure: aria-expanded tells whether the panel it controls is open
  await open(page, 'catalog');
  await tabUntil('button "Filters"');
  console.log(`focus: ${await focused()}`);
  const toggle = page.getByRole('button', { name: 'Filters' });
  await page.keyboard.press('Enter');
  // Angular renders the signal change asynchronously: wait for the attribute before reading the tree
  await toggle.and(page.locator('[aria-expanded="false"]')).waitFor();
  console.log(`Enter: ${await focused()}, radio group visible=${await page.getByRole('group', { name: 'Category' }).isVisible()}`);
  await page.keyboard.press('Space');
  await toggle.and(page.locator('[aria-expanded="true"]')).waitFor();
  console.log(`Space: ${await focused()}, radio group visible=${await page.getByRole('group', { name: 'Category' }).isVisible()}`);
}

await browser.close();
